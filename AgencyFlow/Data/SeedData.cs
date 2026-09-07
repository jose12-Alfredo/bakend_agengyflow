using AgencyFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace AgencyFlow.Data;

public static class SeedData
{
    private static readonly DateTime SeedDate =
        new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static Guid Id(string value) => Guid.Parse(value);

    public static void Configure(ModelBuilder modelBuilder)
    {
        ConfigureRoles(modelBuilder);
        ConfigureDepartments(modelBuilder);
        ConfigureCompanies(modelBuilder);
        ConfigureUsers(modelBuilder);
        ConfigureProjectTypes(modelBuilder);
        ConfigureProjects(modelBuilder);
        ConfigureSubProjects(modelBuilder);
        ConfigureTasks(modelBuilder);
        ConfigureSubTasks(modelBuilder);
        ConfigureDepartmentUsers(modelBuilder);
        ConfigureProjectDirectorAccesses(modelBuilder);
    }

    private static void ConfigureRoles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = Id("11111111-1111-1111-1111-111111111101"), Name = "Cliente", Description = "Usuario externo que representa a una empresa cliente.", CreatedAt = SeedDate },
            new Role { Id = Id("11111111-1111-1111-1111-111111111102"), Name = "Gerente", Description = "Responsable de la gestion general y toma de decisiones.", CreatedAt = SeedDate },
            new Role { Id = Id("11111111-1111-1111-1111-111111111103"), Name = "Director", Description = "Lidera un area o departamento de la empresa.", CreatedAt = SeedDate },
            new Role { Id = Id("11111111-1111-1111-1111-111111111104"), Name = "Operativo 1", Description = "Personal operativo con mayor nivel de experiencia.", CreatedAt = SeedDate },
            new Role { Id = Id("11111111-1111-1111-1111-111111111105"), Name = "Operativo 2", Description = "Personal operativo de nivel inicial.", CreatedAt = SeedDate },
            new Role { Id = Id("11111111-1111-1111-1111-111111111106"), Name = "Pasante", Description = "Estudiante en formacion practica dentro de la empresa.", CreatedAt = SeedDate });
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = Id("11111111-1111-1111-1111-111111111107"), Name = "SuperUsuario", Description = "Administrador tecnico con acceso completo.", CreatedAt = SeedDate });
    }

    private static void ConfigureDepartments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Department>().HasData(
            new Department { Id = Id("22222222-2222-2222-2222-222222222201"), Name = "ATL", Description = "Medios tradicionales: TV, radio, prensa y via publica.", CreatedAt = SeedDate },
            new Department { Id = Id("22222222-2222-2222-2222-222222222202"), Name = "Digital", Description = "Estrategia y ejecucion de campanas en medios digitales.", CreatedAt = SeedDate },
            new Department { Id = Id("22222222-2222-2222-2222-222222222203"), Name = "Diseno", Description = "Diseno grafico y produccion visual de piezas.", CreatedAt = SeedDate },
            new Department { Id = Id("22222222-2222-2222-2222-222222222204"), Name = "Creatividad", Description = "Conceptualizacion creativa de campanas y contenidos.", CreatedAt = SeedDate },
            new Department { Id = Id("22222222-2222-2222-2222-222222222205"), Name = "Content", Description = "Generacion de contenido para redes y plataformas digitales.", CreatedAt = SeedDate },
            new Department { Id = Id("22222222-2222-2222-2222-222222222206"), Name = "Ejecutivo de cuenta", Description = "Gestion y seguimiento directo de las cuentas de clientes.", CreatedAt = SeedDate },
            new Department { Id = Id("22222222-2222-2222-2222-222222222207"), Name = "Gerencia", Description = "Direccion y gestion general de la empresa.", CreatedAt = SeedDate });
    }

    private static void ConfigureCompanies(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClientCompany>().HasData(
            new ClientCompany { Id = Id("33333333-3333-3333-3333-333333333301"), Name = "Honor", Description = "Empresa cliente del sector tecnologia y dispositivos moviles.", CreatedAt = SeedDate },
            new ClientCompany { Id = Id("33333333-3333-3333-3333-333333333302"), Name = "Adidas", Description = "Empresa cliente del sector deportivo y moda.", CreatedAt = SeedDate },
            new ClientCompany { Id = Id("33333333-3333-3333-3333-333333333303"), Name = "Tigo", Description = "Empresa cliente del sector telecomunicaciones.", CreatedAt = SeedDate });
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        const string passwordHash = "DISABLED_DEMO_ACCOUNT";

        modelBuilder.Entity<User>().HasData(
            new User { Id = Id("44444444-4444-4444-4444-444444444401"), FirstName = "Demo", LastName = "Gerencia", Email = "gerencia.demo@example.com", Password = passwordHash, RoleId = Id("11111111-1111-1111-1111-111111111102"), CreatedAt = SeedDate },
            new User { Id = Id("44444444-4444-4444-4444-444444444402"), FirstName = "Demo", LastName = "Directora Digital", Email = "directora.digital@example.com", Password = passwordHash, RoleId = Id("11111111-1111-1111-1111-111111111103"), CreatedAt = SeedDate },
            new User { Id = Id("44444444-4444-4444-4444-444444444403"), FirstName = "Demo", LastName = "Director Creativo", Email = "director.creativo@example.com", Password = passwordHash, RoleId = Id("11111111-1111-1111-1111-111111111103"), CreatedAt = SeedDate },
            new User { Id = Id("44444444-4444-4444-4444-444444444404"), FirstName = "Demo", LastName = "Cliente Honor", RoleId = Id("11111111-1111-1111-1111-111111111101"), ClientCompanyId = Id("33333333-3333-3333-3333-333333333301"), CreatedAt = SeedDate },
            new User { Id = Id("44444444-4444-4444-4444-444444444405"), FirstName = "Demo", LastName = "Cliente Adidas", RoleId = Id("11111111-1111-1111-1111-111111111101"), ClientCompanyId = Id("33333333-3333-3333-3333-333333333302"), CreatedAt = SeedDate },
            new User { Id = Id("44444444-4444-4444-4444-444444444406"), FirstName = "Demo", LastName = "Cliente Tigo", RoleId = Id("11111111-1111-1111-1111-111111111101"), ClientCompanyId = Id("33333333-3333-3333-3333-333333333303"), CreatedAt = SeedDate },
            new User { Id = Id("44444444-4444-4444-4444-444444444407"), FirstName = "Demo", LastName = "Operativo Diseno", Email = "operativo.diseno@example.com", Password = passwordHash, RoleId = Id("11111111-1111-1111-1111-111111111104"), CreatedAt = SeedDate },
            new User { Id = Id("44444444-4444-4444-4444-444444444408"), FirstName = "Demo", LastName = "Operativo Junior", Email = "operativo.junior@example.com", Password = passwordHash, RoleId = Id("11111111-1111-1111-1111-111111111105"), CreatedAt = SeedDate },
            new User { Id = Id("44444444-4444-4444-4444-444444444409"), FirstName = "Demo", LastName = "Operativo Ejecutivo", Email = "operativo.ejecutivo@example.com", Password = passwordHash, RoleId = Id("11111111-1111-1111-1111-111111111104"), CreatedAt = SeedDate },
            new User { Id = Id("44444444-4444-4444-4444-444444444410"), FirstName = "Demo", LastName = "Pasante ATL", Email = "pasante.atl@example.com", Password = passwordHash, RoleId = Id("11111111-1111-1111-1111-111111111104"), CreatedAt = SeedDate });
    }

    private static void ConfigureProjectTypes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProjectType>().HasData(
            new ProjectType { Id = Id("55555555-5555-5555-5555-555555555501"), Name = "Branding", Description = "Proyectos de construccion e identidad de marca.", CreatedAt = SeedDate },
            new ProjectType { Id = Id("55555555-5555-5555-5555-555555555502"), Name = "Marketing", Description = "Estrategias y campanas de marketing en general.", CreatedAt = SeedDate },
            new ProjectType { Id = Id("55555555-5555-5555-5555-555555555503"), Name = "Produccion Audiovisual", Description = "Produccion de video, fotografia y contenido audiovisual.", CreatedAt = SeedDate },
            new ProjectType { Id = Id("55555555-5555-5555-5555-555555555504"), Name = "Medios Publicitarios", Description = "Planificacion y compra de espacios publicitarios.", CreatedAt = SeedDate });
    }

    private static void ConfigureProjects(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>().HasData(
            new Project { Id = Id("66666666-6666-6666-6666-666666666601"), Title = "Campana de lanzamiento Honor X10", Detail = "Campana integral de branding y marketing para el lanzamiento del nuevo modelo.", StartDate = new DateOnly(2026, 6, 1), EndDate = new DateOnly(2026, 8, 31), ProjectTypeId = Id("55555555-5555-5555-5555-555555555501"), ClientUserId = Id("44444444-4444-4444-4444-444444444404"), CreatedAt = SeedDate },
            new Project { Id = Id("66666666-6666-6666-6666-666666666602"), Title = "Spot publicitario Adidas Running", Detail = "Produccion audiovisual de spot para campana de la linea de running.", StartDate = new DateOnly(2026, 7, 1), ProjectTypeId = Id("55555555-5555-5555-5555-555555555503"), ClientUserId = Id("44444444-4444-4444-4444-444444444405"), CreatedAt = SeedDate },
            new Project { Id = Id("66666666-6666-6666-6666-666666666603"), Title = "Plan de medios Tigo Q3", Detail = "Planificacion y compra de espacios publicitarios para el tercer trimestre.", StartDate = new DateOnly(2026, 7, 15), EndDate = new DateOnly(2026, 9, 30), ProjectTypeId = Id("55555555-5555-5555-5555-555555555504"), ClientUserId = Id("44444444-4444-4444-4444-444444444406"), CreatedAt = SeedDate });
    }

    private static void ConfigureSubProjects(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SubProject>().HasData(
            new SubProject { Id = Id("77777777-7777-7777-7777-777777777701"), Title = "Diseno de piezas graficas - Lanzamiento Honor X10", Detail = "Creacion de banners, posts y material grafico para la campana.", StartDate = new DateOnly(2026, 6, 1), EndDate = new DateOnly(2026, 6, 20), Status = "En curso", ProjectId = Id("66666666-6666-6666-6666-666666666601"), DepartmentId = Id("22222222-2222-2222-2222-222222222203"), AssignedUserId = Id("44444444-4444-4444-4444-444444444409"), CreatedAt = SeedDate },
            new SubProject { Id = Id("77777777-7777-7777-7777-777777777702"), Title = "Guion y storyboard - Spot Adidas Running", Detail = "Desarrollo creativo del concepto y guion para el spot audiovisual.", StartDate = new DateOnly(2026, 7, 1), EndDate = new DateOnly(2026, 7, 10), Status = "Pendiente", ProjectId = Id("66666666-6666-6666-6666-666666666602"), DepartmentId = Id("22222222-2222-2222-2222-222222222204"), CreatedAt = SeedDate },
            new SubProject { Id = Id("77777777-7777-7777-7777-777777777703"), Title = "Compra de espacios en medios - Tigo Q3", Detail = "Negociacion y reserva de espacios publicitarios para el trimestre.", StartDate = new DateOnly(2026, 7, 15), EndDate = new DateOnly(2026, 8, 1), Status = "Pendiente", ProjectId = Id("66666666-6666-6666-6666-666666666603"), DepartmentId = Id("22222222-2222-2222-2222-222222222201"), AssignedUserId = Id("44444444-4444-4444-4444-444444444407"), CreatedAt = SeedDate });
    }

    private static void ConfigureTasks(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskItem>().HasData(
            new TaskItem { Id = Id("88888888-8888-8888-8888-888888888801"), Title = "Disenar banner principal para redes", Detail = "Banner para Instagram y Facebook con las dimensiones requeridas por campana.", StartDate = new DateOnly(2026, 6, 1), EndDate = new DateOnly(2026, 6, 5), Status = "En curso", SubProjectId = Id("77777777-7777-7777-7777-777777777701"), AssignedUserId = Id("44444444-4444-4444-4444-444444444409"), CreatedAt = SeedDate },
            new TaskItem { Id = Id("88888888-8888-8888-8888-888888888802"), Title = "Redactar guion del spot", Detail = "Primera version del guion narrativo para revision del cliente.", StartDate = new DateOnly(2026, 7, 1), EndDate = new DateOnly(2026, 7, 4), Status = "Pendiente", SubProjectId = Id("77777777-7777-7777-7777-777777777702"), CreatedAt = SeedDate },
            new TaskItem { Id = Id("88888888-8888-8888-8888-888888888803"), Title = "Cotizar espacios en via publica", Detail = "Solicitar cotizaciones a proveedores de espacios publicitarios.", StartDate = new DateOnly(2026, 7, 15), EndDate = new DateOnly(2026, 7, 20), Status = "Pendiente", SubProjectId = Id("77777777-7777-7777-7777-777777777703"), AssignedUserId = Id("44444444-4444-4444-4444-444444444407"), CreatedAt = SeedDate });
    }

    private static void ConfigureSubTasks(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SubTask>().HasData(
            new SubTask { Id = Id("99999999-9999-9999-9999-999999999901"), Title = "Seleccionar paleta de colores", Detail = "Definir colores acorde a la identidad de marca de Honor.", StartDate = new DateOnly(2026, 6, 1), EndDate = new DateOnly(2026, 6, 2), Status = "Completada", TaskItemId = Id("88888888-8888-8888-8888-888888888801"), AssignedUserId = Id("44444444-4444-4444-4444-444444444409"), CreatedAt = SeedDate },
            new SubTask { Id = Id("99999999-9999-9999-9999-999999999902"), Title = "Investigar referencias narrativas", Detail = "Buscar referencias de spots similares para inspiracion del guion.", StartDate = new DateOnly(2026, 7, 1), EndDate = new DateOnly(2026, 7, 2), Status = "Pendiente", TaskItemId = Id("88888888-8888-8888-8888-888888888802"), CreatedAt = SeedDate },
            new SubTask { Id = Id("99999999-9999-9999-9999-999999999903"), Title = "Contactar proveedores de vallas", Detail = "Armar lista de proveedores y solicitar datos de contacto.", StartDate = new DateOnly(2026, 7, 15), EndDate = new DateOnly(2026, 7, 16), Status = "Pendiente", TaskItemId = Id("88888888-8888-8888-8888-888888888803"), AssignedUserId = Id("44444444-4444-4444-4444-444444444407"), CreatedAt = SeedDate });
    }

    private static void ConfigureDepartmentUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DepartmentUser>().HasData(
            new DepartmentUser { Id = Id("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa08"), UserId = Id("44444444-4444-4444-4444-444444444401"), DepartmentId = Id("22222222-2222-2222-2222-222222222207"), CreatedAt = SeedDate },
            new DepartmentUser { Id = Id("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa01"), UserId = Id("44444444-4444-4444-4444-444444444402"), DepartmentId = Id("22222222-2222-2222-2222-222222222202"), IsDirector = true, CreatedAt = SeedDate },
            new DepartmentUser { Id = Id("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa02"), UserId = Id("44444444-4444-4444-4444-444444444403"), DepartmentId = Id("22222222-2222-2222-2222-222222222203"), IsDirector = true, CreatedAt = SeedDate },
            new DepartmentUser { Id = Id("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa03"), UserId = Id("44444444-4444-4444-4444-444444444403"), DepartmentId = Id("22222222-2222-2222-2222-222222222204"), IsDirector = true, CreatedAt = SeedDate },
            new DepartmentUser { Id = Id("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa04"), UserId = Id("44444444-4444-4444-4444-444444444407"), DepartmentId = Id("22222222-2222-2222-2222-222222222203"), CreatedAt = SeedDate },
            new DepartmentUser { Id = Id("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa05"), UserId = Id("44444444-4444-4444-4444-444444444408"), DepartmentId = Id("22222222-2222-2222-2222-222222222203"), CreatedAt = SeedDate },
            new DepartmentUser { Id = Id("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa06"), UserId = Id("44444444-4444-4444-4444-444444444409"), DepartmentId = Id("22222222-2222-2222-2222-222222222206"), CreatedAt = SeedDate },
            new DepartmentUser { Id = Id("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa07"), UserId = Id("44444444-4444-4444-4444-444444444410"), DepartmentId = Id("22222222-2222-2222-2222-222222222201"), CreatedAt = SeedDate });
    }

    private static void ConfigureProjectDirectorAccesses(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProjectDirectorAccess>().HasData(
            new ProjectDirectorAccess
            {
                Id = Id("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb01"),
                ProjectId = Id("66666666-6666-6666-6666-666666666601"),
                DirectorUserId = Id("44444444-4444-4444-4444-444444444402"),
                GrantedByUserId = Id("44444444-4444-4444-4444-444444444401"),
                CreatedAt = SeedDate
            });
    }
}
