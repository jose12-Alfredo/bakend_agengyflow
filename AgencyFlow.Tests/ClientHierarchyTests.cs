using System.Security.Claims;
using AgencyFlow.Authorization;
using AgencyFlow.Data;
using AgencyFlow.DTOs.Dashboard;
using AgencyFlow.Models;
using AgencyFlow.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace AgencyFlow.Tests;

public class ClientHierarchyTests
{
    [Fact]
    public async Task Returns_empty_projects_and_null_progress_for_client_without_projects()
    {
        await using var db = CreateDb();
        var client = new ClientCompany { Name = "Cliente sin proyectos" };
        db.ClientCompanies.Add(client);
        await db.SaveChangesAsync();

        var result = await CreateService(db)
            .GetClientHierarchyAsync(client.Id, Filter());

        Assert.NotNull(result);
        Assert.Empty(result.Projects);
        Assert.Null(result.ProgressPercentage);

        var dashboard = new DashboardDto();
        await CreateService(db).PopulateAsync(
            dashboard,
            new DashboardFilterDto { Year = 2026, Month = 8 },
            new DateTime(2026, 8, 27, 12, 0, 0, DateTimeKind.Utc));

        var clientHealth = Assert.Single(dashboard.ClientHealth);
        Assert.Equal(client.Id, clientHealth.ClientId);
        Assert.Equal(0, clientHealth.ActiveProjectCount);
        Assert.Null(clientHealth.ProgressPercentage);
    }

    [Fact]
    public async Task Calculates_progress_and_keeps_unassigned_subtask_as_null()
    {
        await using var db = CreateDb();
        var client = new ClientCompany { Name = "Tigo" };
        var role = new Role { Name = "Operativo" };
        var clientRole = new Role { Name = "Cliente" };
        var clientUser = new User { FirstName = "Cliente", LastName = "Uno", Role = clientRole, ClientCompany = client };
        var worker = new User { FirstName = "Gabriel", LastName = "Aguilar", Role = role };
        var type = new ProjectType { Name = "Campaña" };
        var department = new Department { Name = "Diseño" };
        var project = new Project { Title = "Q3", StartDate = new DateOnly(2026, 8, 1), ProjectType = type, ClientUser = clientUser };
        var subProject = new SubProject { Title = "Piezas", StartDate = new DateOnly(2026, 8, 1), Project = project, Department = department };
        var task = new TaskItem { Title = "Banner", StartDate = new DateOnly(2026, 8, 1), AssignedUser = worker, SubProject = subProject };
        task.SubTasks.Add(new SubTask { Title = "Completada", StartDate = new DateOnly(2026, 8, 1), Status = SubTaskStatuses.Completed, AssignedUser = worker });
        task.SubTasks.Add(new SubTask { Title = "Sin responsable", StartDate = new DateOnly(2026, 8, 1), Status = SubTaskStatuses.Pending });
        db.Add(task);
        await db.SaveChangesAsync();

        var result = await CreateService(db)
            .GetClientHierarchyAsync(client.Id, Filter());

        var returnedTask = Assert.Single(Assert.Single(Assert.Single(result!.Projects).SubProjects).Tasks);
        Assert.Equal(50, returnedTask.ProgressPercentage);
        var unassigned = Assert.Single(
            returnedTask.SubTasks,
            item => item.Title == "Sin responsable");
        Assert.Null(unassigned.AssignedUserId);
        Assert.Null(unassigned.AssignedUserName);
        Assert.Equal("Diseño", result.Projects[0].SubProjects[0].DepartmentName);
    }

    [Fact]
    public async Task Progress_includes_all_tasks_of_a_subproject_active_in_the_month()
    {
        await using var db = CreateDb();
        var client = new ClientCompany { Name = "Banco Nacional de Bolivia" };
        var clientRole = new Role { Name = "Cliente" };
        var clientUser = new User
        {
            FirstName = "Cliente",
            LastName = "BNB",
            Role = clientRole,
            ClientCompany = client
        };
        var type = new ProjectType { Name = "Campaña" };
        var department = new Department { Name = "Creatividad" };
        var project = new Project
        {
            Title = "Alcance de metas",
            StartDate = new DateOnly(2026, 8, 1),
            EndDate = new DateOnly(2026, 12, 31),
            ProjectType = type,
            ClientUser = clientUser
        };
        var subProject = new SubProject
        {
            Title = "Diseños de banner",
            StartDate = new DateOnly(2026, 8, 1),
            EndDate = new DateOnly(2026, 12, 31),
            Project = project,
            Department = department
        };
        var completedTask = new TaskItem
        {
            Title = "Banner color blanco",
            StartDate = new DateOnly(2026, 8, 1),
            Status = TaskItemStatuses.Completed,
            SubProject = subProject
        };
        completedTask.SubTasks.Add(new SubTask
        {
            Title = "Pintar banner",
            StartDate = new DateOnly(2026, 8, 1),
            Status = SubTaskStatuses.Completed
        });
        var pendingFutureTask = new TaskItem
        {
            Title = "Banner negro",
            StartDate = new DateOnly(2026, 9, 1),
            Status = TaskItemStatuses.Pending,
            SubProject = subProject
        };
        pendingFutureTask.SubTasks.Add(new SubTask
        {
            Title = "Diseñar banner",
            StartDate = new DateOnly(2026, 9, 1),
            Status = SubTaskStatuses.Pending
        });
        db.AddRange(completedTask, pendingFutureTask);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var hierarchy = await service.GetClientHierarchyAsync(client.Id, Filter());
        var dashboard = new DashboardDto();
        await service.PopulateAsync(
            dashboard,
            new DashboardFilterDto { Year = 2026, Month = 8 },
            new DateTime(2026, 8, 27, 12, 0, 0, DateTimeKind.Utc));

        var returnedSubProject = Assert.Single(
            Assert.Single(hierarchy!.Projects).SubProjects);
        Assert.Equal(2, returnedSubProject.Tasks.Count);
        Assert.Equal(50, returnedSubProject.ProgressPercentage);
        Assert.Equal(50, hierarchy.ProgressPercentage);
        Assert.Equal(50, Assert.Single(dashboard.ClientHealth).ProgressPercentage);
        Assert.Equal(50, dashboard.Kpis.GlobalProgressPercentage);
    }

    private static ClientHierarchyFilterDto Filter() => new() { Year = 2026, Month = 8 };

    private static ManagementDashboardService CreateService(AppDbContext db)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, RoleNames.Manager)
        ], "test");
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };

        return new ManagementDashboardService(
            db,
            new ResourceAuthorizationService(db, accessor));
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
