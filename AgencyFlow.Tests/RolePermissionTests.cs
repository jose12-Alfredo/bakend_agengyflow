using System.Security.Claims;
using AgencyFlow.Authorization;
using AgencyFlow.Data;
using AgencyFlow.DTOs.User;
using AgencyFlow.DTOs.SubProject;
using AgencyFlow.Exceptions;
using AgencyFlow.Models;
using AgencyFlow.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace AgencyFlow.Tests;

public class RolePermissionTests
{
    [Theory]
    [InlineData(RoleNames.Manager)]
    [InlineData(RoleNames.SuperUser)]
    public void Administrators_Have_Full_Access(string role)
    {
        using var db = CreateContext();
        var authorization = Authorization(db, Guid.NewGuid(), role);
        Assert.True(authorization.IsAdministrator);
        Assert.Same(db.Projects, authorization.FilterProjects(db.Projects));
    }

    [Fact]
    public async Task Director_Project_Filter_Happens_Before_Pagination()
    {
        await using var db = CreateContext();
        var director = User("Director");
        var type = new ProjectType { Name = "Digital" };
        var unauthorized = new Project { Title = "A", StartDate = DateOnly.MinValue, ProjectType = type };
        var authorized = new Project { Title = "B", StartDate = DateOnly.MinValue, ProjectType = type };
        db.AddRange(director, unauthorized, authorized);
        db.ProjectDirectorAccesses.Add(new ProjectDirectorAccess { Project = authorized, DirectorUser = director, GrantedByUser = director });
        await db.SaveChangesAsync();
        var auth = Authorization(db, director.Id, RoleNames.Director);
        var result = await auth.FilterProjects(db.Projects.OrderBy(p => p.Title)).Skip(0).Take(1).ToListAsync();
        Assert.Single(result);
        Assert.Equal(authorized.Id, result[0].Id);
    }

    [Fact]
    public async Task Director_Needs_Project_Access_And_Directed_Area()
    {
        await using var db = CreateContext();
        var director = User("Director"); var manager = User("Gerente");
        var project = new Project { Title = "P", StartDate = DateOnly.MinValue, ProjectType = new ProjectType { Name = "T" } };
        var area = new Department { Name = "Area" };
        db.AddRange(director, manager, project, area); await db.SaveChangesAsync();
        var auth = Authorization(db, director.Id, RoleNames.Director);
        await Assert.ThrowsAsync<ForbiddenException>(() => auth.EnsureAreaAsync(project.Id, area.Id));
        db.Add(new ProjectDirectorAccess { ProjectId = project.Id, DirectorUserId = director.Id, GrantedByUserId = manager.Id });
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ForbiddenException>(() => auth.EnsureAreaAsync(project.Id, area.Id));
        db.Add(new DepartmentUser { UserId = director.Id, DepartmentId = area.Id, IsDirector = true }); await db.SaveChangesAsync();
        await auth.EnsureAreaAsync(project.Id, area.Id);
    }

    [Fact]
    public async Task Director_Creates_SubProject_Assigned_To_Self()
    {
        await using var db = CreateContext();
        var director = User(RoleNames.Director);
        var otherUser = User("Operativo 1");
        var manager = User(RoleNames.Manager);
        var area = new Department { Name = "Digital" };
        var project = new Project
        {
            Title = "Campaña",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            ProjectType = new ProjectType { Name = "Marketing" }
        };
        db.AddRange(director, otherUser, manager, area, project);
        await db.SaveChangesAsync();
        db.AddRange(
            new ProjectDirectorAccess
            {
                ProjectId = project.Id,
                DirectorUserId = director.Id,
                GrantedByUserId = manager.Id
            },
            new DepartmentUser
            {
                UserId = director.Id,
                DepartmentId = area.Id,
                IsDirector = true
            },
            new DepartmentUser
            {
                UserId = otherUser.Id,
                DepartmentId = area.Id
            });
        await db.SaveChangesAsync();

        var service = new SubProjectService(
            db,
            Authorization(db, director.Id, RoleNames.Director));
        var created = await service.CreateAsync(new CreateSubProjectDto
        {
            Title = "Planificación de medios",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            ProjectId = project.Id,
            DepartmentId = area.Id,
            AssignedUserId = otherUser.Id
        });

        Assert.Equal(director.Id, created.AssignedUserId);
    }

    [Fact]
    public async Task Assignee_Must_Belong_To_Target_Area_And_Not_Be_Client()
    {
        await using var db = CreateContext();
        var director = User("Director"); var operative = User("Operativo 1"); var area = new Department { Name = "Area" };
        db.AddRange(director, operative, area); await db.SaveChangesAsync();
        var auth = Authorization(db, director.Id, RoleNames.Director);
        await Assert.ThrowsAsync<BusinessValidationException>(() => auth.EnsureAssigneeAsync(operative.Id, area.Id));
        db.Add(new DepartmentUser { UserId = operative.Id, DepartmentId = area.Id }); await db.SaveChangesAsync();
        await auth.EnsureAssigneeAsync(operative.Id, area.Id);
    }

    [Fact]
    public async Task Director_Creates_Allowed_Member_And_Membership_Atomically()
    {
        await using var db = CreateContext();
        var director = User("Director"); var role = new Role { Name = "Pasante" }; var area = new Department { Name = "Area" };
        db.AddRange(director, role, area); await db.SaveChangesAsync();
        db.Add(new DepartmentUser { UserId = director.Id, DepartmentId = area.Id, IsDirector = true }); await db.SaveChangesAsync();
        var service = new DirectorTeamService(db, Authorization(db, director.Id, RoleNames.Director));
        var created = await service.CreateAsync(new CreateTeamMemberDto { FirstName = "Eva", LastName = "Lopez", Email = "eva@test.com", Password = "12345678", RoleId = role.Id, DepartmentId = area.Id });
        Assert.True(await db.DepartmentUsers.AnyAsync(m => m.UserId == created.Id && m.DepartmentId == area.Id && !m.IsDirector));
    }

    [Fact]
    public async Task Director_Cannot_Create_Privileged_Role()
    {
        await using var db = CreateContext();
        var director = User("Director"); var managerRole = new Role { Name = "Gerente" }; var area = new Department { Name = "Area" };
        db.AddRange(director, managerRole, area); await db.SaveChangesAsync();
        db.Add(new DepartmentUser { UserId = director.Id, DepartmentId = area.Id, IsDirector = true }); await db.SaveChangesAsync();
        var service = new DirectorTeamService(db, Authorization(db, director.Id, RoleNames.Director));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.CreateAsync(new CreateTeamMemberDto { FirstName = "X", LastName = "Y", RoleId = managerRole.Id, DepartmentId = area.Id }));
    }

    [Fact]
    public async Task Director_Lists_Only_Eligible_Existing_Users_Outside_The_Area()
    {
        await using var db = CreateContext();
        var director = User("Director");
        var candidate = User("Operativo 1");
        candidate.FirstName = "Yoss";
        candidate.Email = "yoss@test.com";
        var existingMember = User("Pasante");
        var manager = User("Gerente");
        var area = new Department { Name = "Digital" };
        var otherArea = new Department { Name = "Contenido" };
        db.AddRange(director, candidate, existingMember, manager, area, otherArea);
        await db.SaveChangesAsync();
        db.AddRange(
            new DepartmentUser { UserId = director.Id, DepartmentId = area.Id, IsDirector = true },
            new DepartmentUser { UserId = existingMember.Id, DepartmentId = area.Id },
            new DepartmentUser { UserId = candidate.Id, DepartmentId = otherArea.Id });
        await db.SaveChangesAsync();

        var service = new DirectorTeamService(db, Authorization(db, director.Id, RoleNames.Director));
        var result = await service.GetCandidatesAsync(area.Id, "yoss", 1, 20);

        var item = Assert.Single(result.Items);
        Assert.Equal(candidate.Id, item.Id);
        Assert.Equal("Operativo 1", item.RoleName);
        Assert.True(item.BelongsToOtherDepartments);
    }

    [Fact]
    public async Task Director_Adds_Existing_User_Without_Changing_Account_Data()
    {
        await using var db = CreateContext();
        var director = User("Director");
        var candidate = User("Operativo 2");
        candidate.Password = "existing-password-hash";
        candidate.Email = "member@test.com";
        var area = new Department { Name = "Digital" };
        var otherArea = new Department { Name = "Contenido" };
        db.AddRange(director, candidate, area, otherArea);
        await db.SaveChangesAsync();
        var originalRoleId = candidate.RoleId;
        db.AddRange(
            new DepartmentUser { UserId = director.Id, DepartmentId = area.Id, IsDirector = true },
            new DepartmentUser { UserId = candidate.Id, DepartmentId = otherArea.Id });
        await db.SaveChangesAsync();

        var service = new DirectorTeamService(db, Authorization(db, director.Id, RoleNames.Director));
        var (created, conflict) = await service.AddExistingAsync(new AddExistingTeamMemberDto
        {
            UserId = candidate.Id,
            DepartmentId = area.Id
        });

        Assert.False(conflict);
        Assert.NotNull(created);
        Assert.False(created.IsDirector);
        Assert.Equal(originalRoleId, candidate.RoleId);
        Assert.Equal("existing-password-hash", candidate.Password);
        Assert.Equal(2, await db.DepartmentUsers.CountAsync(membership =>
            membership.UserId == candidate.Id && membership.DeletedAt == null));
    }

    [Fact]
    public async Task Director_Cannot_Add_Existing_User_To_An_Area_They_Do_Not_Direct()
    {
        await using var db = CreateContext();
        var director = User("Director");
        var candidate = User("Pasante");
        var directedArea = new Department { Name = "Digital" };
        var unauthorizedArea = new Department { Name = "Ventas" };
        db.AddRange(director, candidate, directedArea, unauthorizedArea);
        await db.SaveChangesAsync();
        db.Add(new DepartmentUser { UserId = director.Id, DepartmentId = directedArea.Id, IsDirector = true });
        await db.SaveChangesAsync();

        var service = new DirectorTeamService(db, Authorization(db, director.Id, RoleNames.Director));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.AddExistingAsync(new AddExistingTeamMemberDto
        {
            UserId = candidate.Id,
            DepartmentId = unauthorizedArea.Id
        }));
    }

    [Fact]
    public async Task Director_Cannot_Add_Existing_User_With_Privileged_Role()
    {
        await using var db = CreateContext();
        var director = User("Director");
        var manager = User("Gerente");
        var area = new Department { Name = "Digital" };
        db.AddRange(director, manager, area);
        await db.SaveChangesAsync();
        db.Add(new DepartmentUser { UserId = director.Id, DepartmentId = area.Id, IsDirector = true });
        await db.SaveChangesAsync();

        var service = new DirectorTeamService(db, Authorization(db, director.Id, RoleNames.Director));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.AddExistingAsync(new AddExistingTeamMemberDto
        {
            UserId = manager.Id,
            DepartmentId = area.Id
        }));
    }

    [Fact]
    public async Task Director_Reactivates_Previous_Membership_Instead_Of_Duplicating_It()
    {
        await using var db = CreateContext();
        var director = User("Director");
        var candidate = User("Operativo 1");
        var area = new Department { Name = "Digital" };
        db.AddRange(director, candidate, area);
        await db.SaveChangesAsync();
        var previousMembership = new DepartmentUser
        {
            UserId = candidate.Id,
            DepartmentId = area.Id,
            IsDirector = true,
            DeletedAt = DateTime.UtcNow.AddDays(-1),
            DeletedBy = "old-admin"
        };
        db.AddRange(
            new DepartmentUser { UserId = director.Id, DepartmentId = area.Id, IsDirector = true },
            previousMembership);
        await db.SaveChangesAsync();

        var service = new DirectorTeamService(db, Authorization(db, director.Id, RoleNames.Director));
        var (created, conflict) = await service.AddExistingAsync(new AddExistingTeamMemberDto
        {
            UserId = candidate.Id,
            DepartmentId = area.Id
        });

        Assert.False(conflict);
        Assert.NotNull(created);
        Assert.Equal(previousMembership.Id, created.Id);
        Assert.Null(previousMembership.DeletedAt);
        Assert.Null(previousMembership.DeletedBy);
        Assert.False(previousMembership.IsDirector);
        Assert.Single(await db.DepartmentUsers.Where(membership =>
            membership.UserId == candidate.Id && membership.DepartmentId == area.Id).ToListAsync());
    }

    [Fact]
    public void Missing_NameIdentifier_Is_Unauthorized()
    {
        using var db = CreateContext();
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, RoleNames.Director)], "test")) };
        var auth = new ResourceAuthorizationService(db, new HttpContextAccessor { HttpContext = context });
        Assert.Throws<UnauthorizedAccessException>(() => auth.UserId);
    }

    private static User User(string role) => new() { FirstName = role, LastName = "Test", Role = new Role { Name = role } };
    private static ResourceAuthorizationService Authorization(AppDbContext db, Guid userId, string role)
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString()), new Claim(ClaimTypes.Role, role)], "test");
        return new ResourceAuthorizationService(db, new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } });
    }
    private static AppDbContext CreateContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
}
