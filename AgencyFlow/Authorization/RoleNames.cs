namespace AgencyFlow.Authorization;

public static class RoleNames
{
    public const string SuperUser = "SuperUsuario";
    public const string Manager = "Gerente";
    public const string Director = "Director";
    public const string Administrators = SuperUser + "," + Manager;
    public const string ProjectManagers = Administrators + "," + Director;
    public const string OperationalUsers = "Operativo 1,Operativo 2,Pasante";
    public const string SubTaskWorkers = ProjectManagers + "," + OperationalUsers;
}
