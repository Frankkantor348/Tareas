using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tareas.Data;

namespace Tareas
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("ConexionSQL")
                ?? throw new InvalidOperationException("Connection string 'ConexionSQL' not found.");

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            // Configurar Identity con roles
            builder.Services.AddDefaultIdentity<IdentityUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

            // ✅ CONFIGURACIÓN CORREGIDA DE COOKIES
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Identity/Account/Login";        // Ruta por defecto de Identity
                options.LogoutPath = "/Identity/Account/Logout";      // Ruta por defecto de Identity
                options.AccessDeniedPath = "/Identity/Account/AccessDenied"; // Ruta por defecto
                options.ReturnUrlParameter = "returnUrl";
            });

            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Crear roles por defecto al iniciar la aplicación
            using (var scope = app.Services.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

                string[] roles = { "Docente", "Estudiante" };

                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(new IdentityRole(role));
                    }
                }

                // Crear un usuario docente por defecto (solo para pruebas)
                string docenteEmail = "docente@edutech.com";
                string docentePassword = "Docente123!";

                if (await userManager.FindByEmailAsync(docenteEmail) == null)
                {
                    var docente = new IdentityUser
                    {
                        UserName = docenteEmail,
                        Email = docenteEmail,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(docente, docentePassword);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(docente, "Docente");
                    }
                }

                // Crear un usuario estudiante por defecto
                string estudianteEmail = "estudiante@edutech.com";
                string estudiantePassword = "Estudiante123!";

                if (await userManager.FindByEmailAsync(estudianteEmail) == null)
                {
                    var estudiante = new IdentityUser
                    {
                        UserName = estudianteEmail,
                        Email = estudianteEmail,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(estudiante, estudiantePassword);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(estudiante, "Estudiante");
                    }
                }
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Inicio}/{action=Dashboard}/{id?}");
            app.MapRazorPages();

            app.Run();
        }
    }
}