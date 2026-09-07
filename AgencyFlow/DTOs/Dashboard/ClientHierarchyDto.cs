namespace AgencyFlow.DTOs.Dashboard;

public class ClientHierarchyFilterDto
{
    public int Year { get; set; }

    public int Month { get; set; }

    public Guid? DepartmentId { get; set; }
    public Guid? ResponsibleUserId { get; set; }
}

public class ClientHierarchyDto
{
    public Guid ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public Guid? RepresentativeUserId { get; set; }
    public string? RepresentativeUserName { get; set; }
    public double? ProgressPercentage { get; set; }
    public List<ClientHierarchyProjectDto> Projects { get; set; } = new();
}

public class ClientHierarchyProjectDto
{
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public double? ProgressPercentage { get; set; }
    public Guid? AssignedUserId { get; set; }
    public string? AssignedUserName { get; set; }
    public List<ClientHierarchySubProjectDto> SubProjects { get; set; } = new();
}

public class ClientHierarchySubProjectDto
{
    public Guid SubProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public double? ProgressPercentage { get; set; }
    public List<ClientHierarchyTaskDto> Tasks { get; set; } = new();
}

public class ClientHierarchyTaskDto
{
    public Guid TaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double? ProgressPercentage { get; set; }
    public Guid? AssignedUserId { get; set; }
    public string? AssignedUserName { get; set; }
    public List<ClientHierarchySubTaskDto> SubTasks { get; set; } = new();
}

public class ClientHierarchySubTaskDto
{
    public Guid SubTaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double? ProgressPercentage { get; set; }
    public Guid? AssignedUserId { get; set; }
    public string? AssignedUserName { get; set; }
}
