using System.Reflection;
using System.Security.Claims;
using AgencyFlow.Authorization;
using AgencyFlow.Controllers;
using AgencyFlow.Data;
using AgencyFlow.DTOs.SubTask;
using AgencyFlow.Exceptions;
using AgencyFlow.Models;
using AgencyFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AgencyFlow.Tests;

public class OperationalSubTaskTests
{
    [Theory]
    [InlineData("Operativo 1")]
    [InlineData("Operativo 2")]
    [InlineData("Pasante")]
    public async Task Operational_User_Only_Lists_Own_Tasks_And_SubTasks(string roleName)
    {
        await using var db = CreateContext();
        var fixture = await CreateWorkAsync(db, roleName);
        var auth = Authorization(db, fixture.Owner.Id, roleName);

        var tasks = await auth.FilterTasks(db.TaskItems).ToListAsync();
        var subTasks = await auth.FilterSubTasks(db.SubTasks).ToListAsync();

        Assert.Single(tasks);
        Assert.Equal(fixture.OwnTask.Id, tasks[0].Id);
        Assert.Single(subTasks);
        Assert.Equal(fixture.OwnSubTask.Id, subTasks[0].Id);
    }

    [Fact]
    public async Task Operator_Changes_Own_SubTask_And_Preserves_Structure()
    {
        await using var db = CreateContext();
        var fixture = await CreateWorkAsync(db, "Operativo 1");
        var service = Service(db, fixture.Owner.Id, "Operativo 1");
        var originalTaskId = fixture.OwnSubTask.TaskItemId;
        var originalAssigneeId = fixture.OwnSubTask.AssignedUserId;
        var originalTitle = fixture.OwnSubTask.Title;
        var originalStartDate = fixture.OwnSubTask.StartDate;

        var result = await service.UpdateStatusAsync(fixture.OwnSubTask.Id, SubTaskStatuses.InProgress);

        Assert.NotNull(result);
        Assert.Equal(SubTaskStatuses.InProgress, result.Status);
        Assert.Equal(originalTaskId, result.TaskItemId);
        Assert.Equal(originalAssigneeId, result.AssignedUserId);
        Assert.Equal(originalTitle, result.Title);
        Assert.Equal(originalStartDate, result.StartDate);
        Assert.True(await db.SubTaskStatusHistories.AnyAsync(h => h.SubTaskId == result.Id &&
            h.FromStatus == SubTaskStatuses.Pending && h.ToStatus == SubTaskStatuses.InProgress));
        Assert.NotNull((await db.SubTasks.FindAsync(result.Id))!.LastActivityAt);
    }

    [Fact]
    public async Task Operator_Gets_Forbidden_For_Foreign_SubTask()
    {
        await using var db = CreateContext();
        var fixture = await CreateWorkAsync(db, "Operativo 1");
        var service = Service(db, fixture.Owner.Id, "Operativo 1");

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.UpdateStatusAsync(fixture.ForeignSubTask.Id, SubTaskStatuses.InProgress));
    }

    [Theory]
    [InlineData(SubTaskStatuses.Pending, SubTaskStatuses.InProgress)]
    [InlineData(SubTaskStatuses.InProgress, SubTaskStatuses.InReview)]
    [InlineData(SubTaskStatuses.InReview, SubTaskStatuses.Completed)]
    public async Task Required_Status_Transitions_Are_Allowed(string from, string to)
    {
        await using var db = CreateContext();
        var fixture = await CreateWorkAsync(db, "Pasante");
        fixture.OwnSubTask.Status = from;
        await db.SaveChangesAsync();

        var result = await Service(db, fixture.Owner.Id, "Pasante")
            .UpdateStatusAsync(fixture.OwnSubTask.Id, to);

        Assert.Equal(to, result!.Status);
    }

    [Fact]
    public async Task Operator_Can_Reopen_An_Own_Completed_SubTask()
    {
        await using var db = CreateContext();
        var fixture = await CreateWorkAsync(db, "Operativo 2");
        var service = Service(db, fixture.Owner.Id, "Operativo 2");
        fixture.OwnSubTask.Status = SubTaskStatuses.Completed;
        await db.SaveChangesAsync();

        var result = await service.UpdateStatusAsync(
            fixture.OwnSubTask.Id, SubTaskStatuses.Pending);

        Assert.Equal(SubTaskStatuses.Pending, result!.Status);
    }

    [Fact]
    public async Task Operator_Cannot_Pause_A_SubTask()
    {
        await using var db = CreateContext();
        var fixture = await CreateWorkAsync(db, "Operativo 2");

        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            Service(db, fixture.Owner.Id, "Operativo 2")
                .UpdateStatusAsync(fixture.OwnSubTask.Id, SubTaskStatuses.Paused));
    }

    [Fact]
    public async Task Operator_Creates_SubTask_Assigned_To_Self_And_Preserves_Status()
    {
        await using var db = CreateContext();
        var fixture = await CreateWorkAsync(db, "Operativo 1");
        var foreignUserId = fixture.ForeignSubTask.AssignedUserId;

        var result = await Service(db, fixture.Owner.Id, "Operativo 1")
            .CreateAsync(new CreateSubTaskDto
            {
                Title = "Preparar brief",
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Status = SubTaskStatuses.InProgress,
                TaskItemId = fixture.OwnTask.Id,
                AssignedUserId = foreignUserId
            });

        Assert.Equal(fixture.Owner.Id, result.AssignedUserId);
        Assert.Equal(fixture.OwnTask.Id, result.TaskItemId);
        Assert.Equal(SubTaskStatuses.InProgress, result.Status);
    }

    [Fact]
    public void Operator_Can_Create_But_Administrative_Endpoints_Remain_Closed()
    {
        AssertRoles<SubTasksController>(nameof(SubTasksController.Create), RoleNames.SubTaskWorkers);
        AssertRoles<SubTasksController>(nameof(SubTasksController.Update), RoleNames.ProjectManagers);
        AssertRoles<SubTasksController>(nameof(SubTasksController.Delete), RoleNames.ProjectManagers);
        AssertRoles<SubTasksController>(nameof(SubTasksController.UpdateAssignee), RoleNames.ProjectManagers);
        AssertRoles<TaskItemsController>(nameof(TaskItemsController.Create), RoleNames.ProjectManagers);
        AssertRoles<TaskItemsController>(nameof(TaskItemsController.Update), RoleNames.ProjectManagers);
        AssertRoles<TaskItemsController>(nameof(TaskItemsController.Delete), RoleNames.ProjectManagers);
        AssertRoles<TaskItemsController>(nameof(TaskItemsController.UpdateAssignee), RoleNames.ProjectManagers);
    }

    [Theory]
    [InlineData(RoleNames.Manager)]
    [InlineData(RoleNames.SuperUser)]
    public async Task Management_Retains_Status_Access(string roleName)
    {
        await using var db = CreateContext();
        var fixture = await CreateWorkAsync(db, roleName);
        var result = await Service(db, fixture.Owner.Id, roleName)
            .UpdateStatusAsync(fixture.ForeignSubTask.Id, SubTaskStatuses.InProgress);
        Assert.Equal(SubTaskStatuses.InProgress, result!.Status);
    }

    [Fact]
    public async Task Director_Retains_Project_And_Area_Authorization()
    {
        await using var db = CreateContext();
        var fixture = await CreateWorkAsync(db, RoleNames.Director);
        db.ProjectDirectorAccesses.Add(new ProjectDirectorAccess
        {
            ProjectId = fixture.Project.Id,
            DirectorUserId = fixture.Owner.Id,
            GrantedByUserId = fixture.Owner.Id
        });
        db.DepartmentUsers.Add(new DepartmentUser
        {
            UserId = fixture.Owner.Id,
            DepartmentId = fixture.Department.Id,
            IsDirector = true
        });
        await db.SaveChangesAsync();

        var result = await Service(db, fixture.Owner.Id, RoleNames.Director)
            .UpdateStatusAsync(fixture.ForeignSubTask.Id, SubTaskStatuses.InProgress);
        Assert.Equal(SubTaskStatuses.InProgress, result!.Status);
    }

    [Fact]
    public async Task Login_With_Valid_Operational_User_Returns_Token()
    {
        await using var db = CreateContext();
        var user = new User
        {
            FirstName = "Yoss", LastName = "Quiroga", Email = "yoss@test.com",
            Password = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = new Role { Name = "Operativo 1" }
        };
        db.Add(user); await db.SaveChangesAsync();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "test-key-that-is-long-enough-for-hmac-sha256-12345",
            ["Jwt:Issuer"] = "AgencyFlow", ["Jwt:Audience"] = "AgencyFlow"
        }).Build();

        var result = await new UserService(db, config).LoginAsync(new()
            { Email = user.Email, Password = "password123" });

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.Equal("Operativo 1", result.RoleName);
    }

    [Fact]
    public async Task Login_With_Corrupt_Hash_Does_Not_Throw_500()
    {
        await using var db = CreateContext();
        db.Add(new User { FirstName = "Bad", LastName = "Hash", Email = "bad@test.com",
            Password = "not-a-bcrypt-hash", Role = new Role { Name = "Pasante" } });
        await db.SaveChangesAsync();
        var result = await new UserService(db, new ConfigurationBuilder().Build())
            .LoginAsync(new() { Email = "bad@test.com", Password = "password123" });
        Assert.Null(result);
    }

    [Fact]
    public async Task Login_With_Missing_Jwt_Key_Does_Not_Throw_500()
    {
        await using var db = CreateContext();
        db.Add(new User { FirstName = "No", LastName = "Key", Email = "nokey@test.com",
            Password = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = new Role { Name = "Operativo 2" } });
        await db.SaveChangesAsync();

        var result = await new UserService(db, new ConfigurationBuilder().Build())
            .LoginAsync(new() { Email = "nokey@test.com", Password = "password123" });

        Assert.Null(result);
    }

    private static void AssertRoles<TController>(string methodName, string expected)
    {
        var method = typeof(TController).GetMethod(methodName)!;
        Assert.Equal(expected, method.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
    }

    private static SubTaskService Service(AppDbContext db, Guid userId, string role) =>
        new(db, new WorkAssignmentService(db), Authorization(db, userId, role));

    private static ResourceAuthorizationService Authorization(AppDbContext db, Guid userId, string role)
    {
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)], "test");
        return new(db, new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        });
    }

    private static async Task<Fixture> CreateWorkAsync(AppDbContext db, string ownerRole)
    {
        var owner = new User { FirstName = "Owner", LastName = "User", Role = new Role { Name = ownerRole } };
        var other = new User { FirstName = "Other", LastName = "User", Role = new Role { Name = "Operativo 1" } };
        var department = new Department { Name = "Digital" };
        var project = new Project { Title = "Project", StartDate = DateOnly.FromDateTime(DateTime.UtcNow), ProjectType = new ProjectType { Name = "Type" } };
        var subProject = new SubProject { Title = "Subproject", StartDate = project.StartDate, Project = project, Department = department };
        var ownTask = new TaskItem { Title = "Own task", StartDate = project.StartDate, SubProject = subProject, AssignedUser = owner };
        var foreignTask = new TaskItem { Title = "Foreign task", StartDate = project.StartDate, SubProject = subProject, AssignedUser = other };
        var ownSubTask = new SubTask { Title = "Own subtask", StartDate = project.StartDate, TaskItem = ownTask, AssignedUser = owner };
        var foreignSubTask = new SubTask { Title = "Foreign subtask", StartDate = project.StartDate, TaskItem = foreignTask, AssignedUser = other };
        db.AddRange(owner, other, department, project, subProject, ownTask, foreignTask, ownSubTask, foreignSubTask);
        await db.SaveChangesAsync();
        return new(owner, department, project, ownTask, ownSubTask, foreignSubTask);
    }

    private static AppDbContext CreateContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed record Fixture(User Owner, Department Department, Project Project,
        TaskItem OwnTask, SubTask OwnSubTask, SubTask ForeignSubTask);
}
