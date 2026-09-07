using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AgencyFlow.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ClientCompanies",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "Email", "Name", "Phone", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("33333333-3333-3333-3333-333333333301"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Empresa cliente del sector tecnologia y dispositivos moviles.", null, "Honor", null, null, null },
                    { new Guid("33333333-3333-3333-3333-333333333302"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Empresa cliente del sector deportivo y moda.", null, "Adidas", null, null, null },
                    { new Guid("33333333-3333-3333-3333-333333333303"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Empresa cliente del sector telecomunicaciones.", null, "Tigo", null, null, null }
                });

            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "Name", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-222222222201"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Medios tradicionales: TV, radio, prensa y via publica.", "ATL", null, null },
                    { new Guid("22222222-2222-2222-2222-222222222202"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Estrategia y ejecucion de campanas en medios digitales.", "Digital", null, null },
                    { new Guid("22222222-2222-2222-2222-222222222203"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Diseno grafico y produccion visual de piezas.", "Diseno", null, null },
                    { new Guid("22222222-2222-2222-2222-222222222204"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Conceptualizacion creativa de campanas y contenidos.", "Creatividad", null, null },
                    { new Guid("22222222-2222-2222-2222-222222222205"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Generacion de contenido para redes y plataformas digitales.", "Content", null, null },
                    { new Guid("22222222-2222-2222-2222-222222222206"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Gestion y seguimiento directo de las cuentas de clientes.", "Ejecutivo de cuenta", null, null },
                    { new Guid("22222222-2222-2222-2222-222222222207"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Direccion y gestion general de la empresa.", "Gerencia", null, null }
                });

            migrationBuilder.InsertData(
                table: "ProjectTypes",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "Name", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("55555555-5555-5555-5555-555555555501"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Proyectos de construccion e identidad de marca.", "Branding", null, null },
                    { new Guid("55555555-5555-5555-5555-555555555502"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Estrategias y campanas de marketing en general.", "Marketing", null, null },
                    { new Guid("55555555-5555-5555-5555-555555555503"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Produccion de video, fotografia y contenido audiovisual.", "Produccion Audiovisual", null, null },
                    { new Guid("55555555-5555-5555-5555-555555555504"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Planificacion y compra de espacios publicitarios.", "Medios Publicitarios", null, null }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "Name", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111101"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Usuario externo que representa a una empresa cliente.", "Cliente", null, null },
                    { new Guid("11111111-1111-1111-1111-111111111102"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Responsable de la gestion general y toma de decisiones.", "Gerente", null, null },
                    { new Guid("11111111-1111-1111-1111-111111111103"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Lidera un area o departamento de la empresa.", "Director", null, null },
                    { new Guid("11111111-1111-1111-1111-111111111104"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Personal operativo con mayor nivel de experiencia.", "Operativo 1", null, null },
                    { new Guid("11111111-1111-1111-1111-111111111105"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Personal operativo de nivel inicial.", "Operativo 2", null, null },
                    { new Guid("11111111-1111-1111-1111-111111111106"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Estudiante en formacion practica dentro de la empresa.", "Pasante", null, null }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "ClientCompanyId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Email", "FirstName", "LastName", "Password", "Phone", "RoleId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("44444444-4444-4444-4444-444444444401"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "gerencia.demo@example.com", "Demo", "Gerencia", "DISABLED_DEMO_ACCOUNT", null, new Guid("11111111-1111-1111-1111-111111111102"), null, null },
                    { new Guid("44444444-4444-4444-4444-444444444402"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "directora.digital@example.com", "Demo", "Directora Digital", "DISABLED_DEMO_ACCOUNT", null, new Guid("11111111-1111-1111-1111-111111111103"), null, null },
                    { new Guid("44444444-4444-4444-4444-444444444403"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "director.creativo@example.com", "Demo", "Director Creativo", "DISABLED_DEMO_ACCOUNT", null, new Guid("11111111-1111-1111-1111-111111111103"), null, null },
                    { new Guid("44444444-4444-4444-4444-444444444404"), new Guid("33333333-3333-3333-3333-333333333301"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, "Demo", "Cliente Honor", null, null, new Guid("11111111-1111-1111-1111-111111111101"), null, null },
                    { new Guid("44444444-4444-4444-4444-444444444405"), new Guid("33333333-3333-3333-3333-333333333302"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, "Demo", "Cliente Adidas", null, null, new Guid("11111111-1111-1111-1111-111111111101"), null, null },
                    { new Guid("44444444-4444-4444-4444-444444444406"), new Guid("33333333-3333-3333-3333-333333333303"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, "Demo", "Cliente Tigo", null, null, new Guid("11111111-1111-1111-1111-111111111101"), null, null },
                    { new Guid("44444444-4444-4444-4444-444444444407"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "operativo.diseno@example.com", "Demo", "Operativo Diseno", "DISABLED_DEMO_ACCOUNT", null, new Guid("11111111-1111-1111-1111-111111111104"), null, null },
                    { new Guid("44444444-4444-4444-4444-444444444408"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "operativo.junior@example.com", "Demo", "Operativo Junior", "DISABLED_DEMO_ACCOUNT", null, new Guid("11111111-1111-1111-1111-111111111105"), null, null },
                    { new Guid("44444444-4444-4444-4444-444444444409"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "operativo.ejecutivo@example.com", "Demo", "Operativo Ejecutivo", "DISABLED_DEMO_ACCOUNT", null, new Guid("11111111-1111-1111-1111-111111111104"), null, null },
                    { new Guid("44444444-4444-4444-4444-444444444410"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "pasante.atl@example.com", "Demo", "Pasante ATL", "DISABLED_DEMO_ACCOUNT", null, new Guid("11111111-1111-1111-1111-111111111104"), null, null }
                });

            migrationBuilder.InsertData(
                table: "DepartmentUsers",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DepartmentId", "UpdatedAt", "UpdatedBy", "UserId" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa01"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, new Guid("22222222-2222-2222-2222-222222222202"), null, null, new Guid("44444444-4444-4444-4444-444444444402") },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa02"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, new Guid("22222222-2222-2222-2222-222222222203"), null, null, new Guid("44444444-4444-4444-4444-444444444403") },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa03"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, new Guid("22222222-2222-2222-2222-222222222204"), null, null, new Guid("44444444-4444-4444-4444-444444444403") },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa04"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, new Guid("22222222-2222-2222-2222-222222222203"), null, null, new Guid("44444444-4444-4444-4444-444444444407") },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa05"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, new Guid("22222222-2222-2222-2222-222222222203"), null, null, new Guid("44444444-4444-4444-4444-444444444408") },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa06"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, new Guid("22222222-2222-2222-2222-222222222206"), null, null, new Guid("44444444-4444-4444-4444-444444444409") },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa07"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, new Guid("22222222-2222-2222-2222-222222222201"), null, null, new Guid("44444444-4444-4444-4444-444444444410") },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa08"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, new Guid("22222222-2222-2222-2222-222222222207"), null, null, new Guid("44444444-4444-4444-4444-444444444401") }
                });

            migrationBuilder.InsertData(
                table: "Projects",
                columns: new[] { "Id", "ClientUserId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Detail", "EndDate", "ProjectTypeId", "StartDate", "Title", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("66666666-6666-6666-6666-666666666601"), new Guid("44444444-4444-4444-4444-444444444404"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Campana integral de branding y marketing para el lanzamiento del nuevo modelo.", new DateOnly(2026, 8, 31), new Guid("55555555-5555-5555-5555-555555555501"), new DateOnly(2026, 6, 1), "Campana de lanzamiento Honor X10", null, null },
                    { new Guid("66666666-6666-6666-6666-666666666602"), new Guid("44444444-4444-4444-4444-444444444405"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Produccion audiovisual de spot para campana de la linea de running.", null, new Guid("55555555-5555-5555-5555-555555555503"), new DateOnly(2026, 7, 1), "Spot publicitario Adidas Running", null, null },
                    { new Guid("66666666-6666-6666-6666-666666666603"), new Guid("44444444-4444-4444-4444-444444444406"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Planificacion y compra de espacios publicitarios para el tercer trimestre.", new DateOnly(2026, 9, 30), new Guid("55555555-5555-5555-5555-555555555504"), new DateOnly(2026, 7, 15), "Plan de medios Tigo Q3", null, null }
                });

            migrationBuilder.InsertData(
                table: "SubProjects",
                columns: new[] { "Id", "AssignedUserId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DepartmentId", "Detail", "EndDate", "ProjectId", "StartDate", "Status", "Title", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("77777777-7777-7777-7777-777777777701"), new Guid("44444444-4444-4444-4444-444444444409"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, new Guid("22222222-2222-2222-2222-222222222203"), "Creacion de banners, posts y material grafico para la campana.", new DateOnly(2026, 6, 20), new Guid("66666666-6666-6666-6666-666666666601"), new DateOnly(2026, 6, 1), "En curso", "Diseno de piezas graficas - Lanzamiento Honor X10", null, null },
                    { new Guid("77777777-7777-7777-7777-777777777702"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, new Guid("22222222-2222-2222-2222-222222222204"), "Desarrollo creativo del concepto y guion para el spot audiovisual.", new DateOnly(2026, 7, 10), new Guid("66666666-6666-6666-6666-666666666602"), new DateOnly(2026, 7, 1), "Pendiente", "Guion y storyboard - Spot Adidas Running", null, null },
                    { new Guid("77777777-7777-7777-7777-777777777703"), new Guid("44444444-4444-4444-4444-444444444407"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, new Guid("22222222-2222-2222-2222-222222222201"), "Negociacion y reserva de espacios publicitarios para el trimestre.", new DateOnly(2026, 8, 1), new Guid("66666666-6666-6666-6666-666666666603"), new DateOnly(2026, 7, 15), "Pendiente", "Compra de espacios en medios - Tigo Q3", null, null }
                });

            migrationBuilder.InsertData(
                table: "TaskItems",
                columns: new[] { "Id", "AssignedUserId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Detail", "EndDate", "StartDate", "Status", "SubProjectId", "Title", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("88888888-8888-8888-8888-888888888801"), new Guid("44444444-4444-4444-4444-444444444409"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Banner para Instagram y Facebook con las dimensiones requeridas por campana.", new DateOnly(2026, 6, 5), new DateOnly(2026, 6, 1), "En curso", new Guid("77777777-7777-7777-7777-777777777701"), "Disenar banner principal para redes", null, null },
                    { new Guid("88888888-8888-8888-8888-888888888802"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Primera version del guion narrativo para revision del cliente.", new DateOnly(2026, 7, 4), new DateOnly(2026, 7, 1), "Pendiente", new Guid("77777777-7777-7777-7777-777777777702"), "Redactar guion del spot", null, null },
                    { new Guid("88888888-8888-8888-8888-888888888803"), new Guid("44444444-4444-4444-4444-444444444407"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Solicitar cotizaciones a proveedores de espacios publicitarios.", new DateOnly(2026, 7, 20), new DateOnly(2026, 7, 15), "Pendiente", new Guid("77777777-7777-7777-7777-777777777703"), "Cotizar espacios en via publica", null, null }
                });

            migrationBuilder.InsertData(
                table: "SubTasks",
                columns: new[] { "Id", "AssignedUserId", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Detail", "EndDate", "StartDate", "Status", "TaskItemId", "Title", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("99999999-9999-9999-9999-999999999901"), new Guid("44444444-4444-4444-4444-444444444409"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Definir colores acorde a la identidad de marca de Honor.", new DateOnly(2026, 6, 2), new DateOnly(2026, 6, 1), "Completado", new Guid("88888888-8888-8888-8888-888888888801"), "Seleccionar paleta de colores", null, null },
                    { new Guid("99999999-9999-9999-9999-999999999902"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Buscar referencias de spots similares para inspiracion del guion.", new DateOnly(2026, 7, 2), new DateOnly(2026, 7, 1), "Pendiente", new Guid("88888888-8888-8888-8888-888888888802"), "Investigar referencias narrativas", null, null },
                    { new Guid("99999999-9999-9999-9999-999999999903"), new Guid("44444444-4444-4444-4444-444444444407"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Armar lista de proveedores y solicitar datos de contacto.", new DateOnly(2026, 7, 16), new DateOnly(2026, 7, 15), "Pendiente", new Guid("88888888-8888-8888-8888-888888888803"), "Contactar proveedores de vallas", null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DepartmentUsers",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa01"));

            migrationBuilder.DeleteData(
                table: "DepartmentUsers",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa02"));

            migrationBuilder.DeleteData(
                table: "DepartmentUsers",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa03"));

            migrationBuilder.DeleteData(
                table: "DepartmentUsers",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa04"));

            migrationBuilder.DeleteData(
                table: "DepartmentUsers",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa05"));

            migrationBuilder.DeleteData(
                table: "DepartmentUsers",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa06"));

            migrationBuilder.DeleteData(
                table: "DepartmentUsers",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa07"));

            migrationBuilder.DeleteData(
                table: "DepartmentUsers",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa08"));

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222205"));

            migrationBuilder.DeleteData(
                table: "ProjectTypes",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555502"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"));

            migrationBuilder.DeleteData(
                table: "SubTasks",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999901"));

            migrationBuilder.DeleteData(
                table: "SubTasks",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999902"));

            migrationBuilder.DeleteData(
                table: "SubTasks",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999903"));

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222202"));

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222206"));

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222207"));

            migrationBuilder.DeleteData(
                table: "TaskItems",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888801"));

            migrationBuilder.DeleteData(
                table: "TaskItems",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888802"));

            migrationBuilder.DeleteData(
                table: "TaskItems",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888803"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444401"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444402"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444403"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444408"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444410"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"));

            migrationBuilder.DeleteData(
                table: "SubProjects",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777701"));

            migrationBuilder.DeleteData(
                table: "SubProjects",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777702"));

            migrationBuilder.DeleteData(
                table: "SubProjects",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777703"));

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222201"));

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222203"));

            migrationBuilder.DeleteData(
                table: "Departments",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222204"));

            migrationBuilder.DeleteData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666601"));

            migrationBuilder.DeleteData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666602"));

            migrationBuilder.DeleteData(
                table: "Projects",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666603"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444407"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444409"));

            migrationBuilder.DeleteData(
                table: "ProjectTypes",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555501"));

            migrationBuilder.DeleteData(
                table: "ProjectTypes",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555503"));

            migrationBuilder.DeleteData(
                table: "ProjectTypes",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555504"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444404"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444405"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444406"));

            migrationBuilder.DeleteData(
                table: "ClientCompanies",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333301"));

            migrationBuilder.DeleteData(
                table: "ClientCompanies",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333302"));

            migrationBuilder.DeleteData(
                table: "ClientCompanies",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333303"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"));
        }
    }
}
