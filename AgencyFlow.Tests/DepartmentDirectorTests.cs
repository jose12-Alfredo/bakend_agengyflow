using AgencyFlow.Data;
using AgencyFlow.DTOs.DepartmentUser;
using AgencyFlow.Exceptions;
using AgencyFlow.Models;
using AgencyFlow.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AgencyFlow.Tests;

public class DepartmentDirectorTests
{
    [Fact]
    public async Task Area_Allows_Multiple_Directors()
    {
        await using var db = CreateContext();
        var directorRole = new Role { Name = "Director" };
        var department = new Department { Name = "Digital" };
        var firstDirector = new User
        {
            FirstName = "Ana",
            LastName = "Perez",
            Role = directorRole
        };
        var secondDirector = new User
        {
            FirstName = "Luis",
            LastName = "Rojas",
            Role = directorRole
        };

        db.AddRange(directorRole, department, firstDirector, secondDirector);
        await db.SaveChangesAsync();

        var service = new DepartmentUserService(db);
        await service.CreateAsync(new CreateDepartmentUserDto
        {
            UserId = firstDirector.Id,
            DepartmentId = department.Id,
            IsDirector = true
        });
        await service.CreateAsync(new CreateDepartmentUserDto
        {
            UserId = secondDirector.Id,
            DepartmentId = department.Id,
            IsDirector = true
        });

        var result = await service.GetAllAsync(
            1, 10, null, department.Id, true);

        Assert.Equal(2, result.TotalItems);
        Assert.All(result.Items, membership => Assert.True(membership.IsDirector));
    }

    [Fact]
    public async Task Non_Director_Role_Cannot_Be_Marked_As_Director()
    {
        await using var db = CreateContext();
        var operativeRole = new Role { Name = "Operativo 1" };
        var department = new Department { Name = "Creatividad" };
        var user = new User
        {
            FirstName = "Mario",
            LastName = "Lopez",
            Role = operativeRole
        };

        db.AddRange(operativeRole, department, user);
        await db.SaveChangesAsync();

        var service = new DepartmentUserService(db);

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(
            () => service.CreateAsync(new CreateDepartmentUserDto
            {
                UserId = user.Id,
                DepartmentId = department.Id,
                IsDirector = true
            }));

        Assert.Contains("rol Director", exception.Message);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
