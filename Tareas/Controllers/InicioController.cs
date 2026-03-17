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
                    Calificacion = entrega?.Calificacion
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

        // Acción para la privacidad (mantenida del original)
        public IActionResult Privacy()
        {
            return View();
        }

        // Acción para manejar errores
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