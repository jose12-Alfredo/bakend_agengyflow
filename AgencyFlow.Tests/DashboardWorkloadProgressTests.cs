using AgencyFlow.DTOs.Dashboard;
using AgencyFlow.Models;
using AgencyFlow.Services;
using Xunit;

namespace AgencyFlow.Tests;

public class DashboardWorkloadProgressTests
{
    [Fact]
    public void Uses_Active_Task_Average_When_Active_Tasks_Exist()
    {
        var firstTaskId = Guid.NewGuid();
        var secondTaskId = Guid.NewGuid();
        var tasks = new[]
        {
            new WorkloadTaskDto { TaskId = firstTaskId, ProgressPercentage = 25 },
            new WorkloadTaskDto { TaskId = secondTaskId, ProgressPercentage = 75 }
        };
        var subTasks = new[]
        {
            SubTask(firstTaskId, SubTaskStatuses.Completed),
            SubTask(secondTaskId, SubTaskStatuses.Pending)
        };

        var result = DashboardService.CalculateWorkloadProgress(tasks, subTasks);

        Assert.Equal(50, result);
    }

    [Fact]
    public void Combines_Active_Tasks_With_Independent_SubTasks_Without_Double_Counting()
    {
        var activeTaskId = Guid.NewGuid();
        var otherTaskId = Guid.NewGuid();
        var tasks = new[]
        {
            new WorkloadTaskDto
            {
                TaskId = activeTaskId,
                ProgressPercentage = 0
            }
        };
        var subTasks = new[]
        {
            SubTask(activeTaskId, SubTaskStatuses.Pending),
            SubTask(otherTaskId, SubTaskStatuses.Completed),
            SubTask(Guid.NewGuid(), SubTaskStatuses.Completed),
            SubTask(Guid.NewGuid(), SubTaskStatuses.Completed)
        };

        var result = DashboardService.CalculateWorkloadProgress(tasks, subTasks);

        Assert.Equal(75, result);
    }

    [Fact]
    public void Falls_Back_To_Assigned_SubTask_Completion_When_No_Active_Tasks_Exist()
    {
        var result = DashboardService.CalculateWorkloadProgress(
            Array.Empty<WorkloadTaskDto>(),
            SubTasks(completed: 3, pending: 1));

        Assert.Equal(75, result);
    }

    [Fact]
    public void Returns_Zero_When_No_Work_Is_Assigned()
    {
        var result = DashboardService.CalculateWorkloadProgress(
            Array.Empty<WorkloadTaskDto>(),
            Array.Empty<WorkloadSubTaskDto>());

        Assert.Equal(0, result);
    }

    private static WorkloadSubTaskDto[] SubTasks(int completed, int pending) =>
        Enumerable.Repeat(SubTaskStatuses.Completed, completed)
            .Concat(Enumerable.Repeat(SubTaskStatuses.Pending, pending))
            .Select(status => SubTask(Guid.NewGuid(), status))
            .ToArray();

    private static WorkloadSubTaskDto SubTask(Guid parentTaskId, string status) =>
        new() { ParentTaskId = parentTaskId, Status = status };
}
