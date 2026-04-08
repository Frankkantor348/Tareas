using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;
using Tareas.Data;
using Tareas.Models;
using Tareas.Models.ViewModels;

namespace Tareas.Controllers
{
    [Authorize]
    public class InicioController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public InicioController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Inicio/Dashboard (Redirige según el rol)
        public async Task<IActionResult> Dashboard()
        {
            if (User.IsInRole("Docente"))
            {
                return await DashboardDocente();
            }
            else if (User.IsInRole("Estudiante"))
            {
                return await DashboardEstudiante();
            }

            return RedirectToAction("Index", "Home");
        }

        // Dashboard para docente
        private async Task<IActionResult> DashboardDocente()
        {
            var docenteId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var tareas = await _context.Tareas
                .Where(t => t.DocenteId == docenteId)
                .OrderByDescending(t => t.FechaPublicacion)
                .ToListAsync();

            var tareasIds = tareas.Select(t => t.Id).ToList();

            var entregasPorTarea = await _context.Entregas
                .Where(e => tareasIds.Contains(e.TareaId))
                .GroupBy(e => e.TareaId)
                .Select(g => new { TareaId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.TareaId, g => g.Count);

            var totalEstudiantes = await _userManager.Users.CountAsync();

            var model = new DashboardDocenteViewModel
            {
                TotalTareasPublicadas = tareas.Count,
                TotalEntregasPendientes = await _context.Entregas.CountAsync(e => e.Calificacion == null),
                TotalEstudiantes = totalEstudiantes,
                TareasRecientes = tareas.Take(5).Select(t => new TareaResumenViewModel
                {
                    Id = t.Id,
                    Titulo = t.Titulo,
                    FechaLimite = t.FechaLimite,
                    EntregasRealizadas = entregasPorTarea.ContainsKey(t.Id) ? entregasPorTarea[t.Id] : 0,
                    EntregasPendientes = totalEstudiantes - (entregasPorTarea.ContainsKey(t.Id) ? entregasPorTarea[t.Id] : 0)
                }).ToList()
            };

            return View("DashboardDocente", model);
        }

        // Dashboard para estudiante
        private async Task<IActionResult> DashboardEstudiante()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var tareas = await _context.Tareas
                .OrderBy(t => t.FechaLimite)
                .ToListAsync();

            var entregasRealizadas = await _context.Entregas
                .Where(e => e.EstudianteId == userId)
                .Select(e => new { e.TareaId, e.Calificacion, e.FechaEntrega })
                .ToDictionaryAsync(e => e.TareaId, e => e);

            var hoy = DateTime.Now.Date;

            var tareasViewModel = tareas.Select(t =>
            {
                entregasRealizadas.TryGetValue(t.Id, out var entrega);
                return new TareaEstudianteViewModel
                {
                    Id = t.Id,
                    Titulo = t.Titulo,
                    Descripcion = t.Descripcion,
                    FechaPublicacion = t.FechaPublicacion,
                    FechaLimite = t.FechaLimite,
                    ColorSemaforo = t.ColorSemaforo,
                    Entregada = entrega != null,
                    FechaEntrega = entrega?.FechaEntrega,
                    Calificada = entrega?.Calificacion.HasValue ?? false,
                    Calificacion = entrega?.Calificacion,
                    RutaArchivoApoyo = t.RutaArchivoApoyo,
                    NombreArchivoApoyo = t.NombreArchivoApoyo,
                    TipoArchivoApoyo = t.TipoArchivoApoyo
                };
            }).ToList();

            var model = new DashboardEstudianteViewModel
            {
                TareasPendientes = tareasViewModel.Count(t => !t.Entregada && t.FechaLimite.Date >= hoy),
                TareasEntregadas = tareasViewModel.Count(t => t.Entregada && !t.Calificada),
                TareasCalificadas = tareasViewModel.Count(t => t.Calificada),
                TareasVencidas = tareasViewModel.Count(t => !t.Entregada && t.FechaLimite.Date < hoy),
                TareasProximas = tareasViewModel
                    .Where(t => !t.Entregada && t.FechaLimite.Date >= hoy)
                    .OrderBy(t => t.FechaLimite)
                    .Take(5)
                    .ToList()
            };

            return View("DashboardEstudiante", model);
        }

        // NUEVO: Dashboard de estadísticas (solo para docentes)
        [Authorize(Roles = "Docente")]
        public async Task<IActionResult> DashboardEstadisticas()
        {
            var model = new DashboardStatsViewModel();

            // Total de usuarios por rol
            var estudiantes = await _userManager.GetUsersInRoleAsync("Estudiante");
            var docentes = await _userManager.GetUsersInRoleAsync("Docente");

            model.TotalEstudiantes = estudiantes.Count;
            model.TotalDocentes = docentes.Count;

            // Total de tareas y entregas
            model.TotalTareas = await _context.Tareas.CountAsync();
            model.TotalEntregas = await _context.Entregas.CountAsync();
            model.EntregasPendientes = await _context.Entregas.CountAsync(e => e.Calificacion == null);
            model.EntregasCalificadas = await _context.Entregas.CountAsync(e => e.Calificacion != null);

            // Tareas por estado (semáforo)
            var tareas = await _context.Tareas.ToListAsync();
            model.TareasPorEstado = tareas
                .GroupBy(t => t.ColorSemaforo)
                .Select(g => new ChartData
                {
                    Label = g.Key switch
                    {
                        "verde" => "Con tiempo",
                        "amarillo" => "Próximas a vencer",
                        "rojo" => "Vencidas",
                        _ => g.Key
                    },
                    Value = g.Count(),
                    Color = g.Key switch
                    {
                        "verde" => "#28a745",
                        "amarillo" => "#ffc107",
                        "rojo" => "#dc3545",
                        _ => "#6c757d"
                    }
                }).ToList();

            // Si no hay tareas, agregar datos por defecto
            if (!model.TareasPorEstado.Any())
            {
                model.TareasPorEstado.Add(new ChartData { Label = "Sin tareas", Value = 1, Color = "#6c757d" });
            }

            // Entregas de los últimos 7 días
            model.EntregasPorDia = new List<ChartData>();
            for (int i = 6; i >= 0; i--)
            {
                var fecha = DateTime.Now.Date.AddDays(-i);
                var entregas = await _context.Entregas.CountAsync(e => e.FechaEntrega.Date == fecha);
                model.EntregasPorDia.Add(new ChartData
                {
                    Label = fecha.ToString("dd/MM"),
                    Value = entregas
                });
            }

            // 🔥 CALIFICACIONES POR CURSO - CORREGIDO
            var cursos = await _context.Tareas
                .Where(t => t.Curso != null && t.Entregas.Any(e => e.Calificacion.HasValue))
                .Select(t => t.Curso)
                .Distinct()
                .ToListAsync();

            model.CalificacionesPromedioPorCurso = new List<ChartData>();

            foreach (var curso in cursos.Take(5))
            {
                var entregasCalificadas = await _context.Entregas
    .Include(e => e.Tarea)
    .Where(e => e.Tarea != null && e.Tarea.Curso == curso && e.Calificacion.HasValue)
    .Select(e => e.Calificacion!.Value)
    .ToListAsync();
                if (entregasCalificadas.Any())
                {
                    var promedio = (int)Math.Round(entregasCalificadas.Average());
                    model.CalificacionesPromedioPorCurso.Add(new ChartData
                    {
                        Label = curso ?? "Sin curso",
                        Value = promedio,
                        Color = ObtenerColorAleatorio()
                    });
                }
            }

            // Si no hay calificaciones, agregar dato por defecto
            if (!model.CalificacionesPromedioPorCurso.Any())
            {
                model.CalificacionesPromedioPorCurso.Add(new ChartData
                {
                    Label = "Sin datos",
                    Value = 0,
                    Color = "#6c757d"
                });
            }

            return View(model);
        }

        // Método auxiliar para generar colores aleatorios
        private string ObtenerColorAleatorio()
        {
            var random = new Random();
            var colores = new[] { "#007bff", "#28a745", "#ffc107", "#17a2b8", "#6610f2", "#e83e8c", "#fd7e14" };
            return colores[random.Next(colores.Length)];
        }

        // Acción para la privacidad (mantenida del original)
        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        // Acción para manejar errores
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}