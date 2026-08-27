using Microsoft.EntityFrameworkCore;
using AgencyFlow.Models;

namespace AgencyFlow.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) 
        : base(options)
    {
    }
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<ClientCompany> ClientCompanies => Set<ClientCompany>();
    public DbSet<ProjectType> ProjectTypes => Set<ProjectType>();
    public DbSet<User> Users => Set<User>();
    public DbSet<DepartmentUser> DepartmentUsers => Set<DepartmentUser>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<SubProject> SubProjects => Set<SubProject>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<SubTask> SubTasks => Set<SubTask>();
    public DbSet<SubTaskComment> SubTaskComments => Set<SubTaskComment>();
    public DbSet<TaskStatusHistory> TaskStatusHistories => Set<TaskStatusHistory>();
    public DbSet<SubTaskStatusHistory> SubTaskStatusHistories => Set<SubTaskStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<DepartmentUser>()
            .HasIndex(du => new { du.UserId, du.DepartmentId })
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");
        modelBuilder.Entity<User>()
            .HasIndex(user => user.Email)
            .IsUnique()
            .HasFilter("\"Email\" IS NOT NULL AND \"DeletedAt\" IS NULL");

        modelBuilder.Entity<ClientCompany>()
            .HasOne(company => company.RepresentativeUser)
            .WithMany()
            .HasForeignKey(company => company.RepresentativeUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<TaskStatusHistory>(entity =>
        {
            entity.ToTable("task_status_history");
            entity.Property(history => history.Id).HasColumnName("id");
            entity.Property(history => history.TaskId).HasColumnName("task_id");
            entity.Property(history => history.FromStatus).HasColumnName("from_status");
            entity.Property(history => history.ToStatus).HasColumnName("to_status");
            entity.Property(history => history.Timestamp).HasColumnName("timestamp");

            entity.HasIndex(history => new { history.TaskId, history.Timestamp });
            entity.HasOne(history => history.Task)
                .WithMany(task => task.StatusHistory)
                .HasForeignKey(history => history.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SubTaskStatusHistory>(entity =>
        {
            entity.ToTable("subtask_status_history");
            entity.Property(history => history.Id).HasColumnName("id");
            entity.Property(history => history.SubTaskId).HasColumnName("subtask_id");
            entity.Property(history => history.FromStatus).HasColumnName("from_status");
            entity.Property(history => history.ToStatus).HasColumnName("to_status");
            entity.Property(history => history.Timestamp).HasColumnName("timestamp");

            entity.HasIndex(history => new { history.SubTaskId, history.Timestamp });
            entity.HasOne(history => history.SubTask)
                .WithMany(subTask => subTask.StatusHistory)
                .HasForeignKey(history => history.SubTaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        SeedData.Configure(modelBuilder);
    }
    
}

