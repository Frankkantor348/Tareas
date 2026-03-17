using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Tareas.Data;
using Tareas.Models;
using Tareas.Models.ViewModels;

namespace Tareas.Controllers
{
    [Authorize]
    public class TareasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public TareasController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Tareas (Vista para estudiantes - lista de tareas disponibles)
        [Authorize(Roles = "Estudiante")]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var tareas = await _context.Tareas
                .Where(t => t.FechaLimite.Date >= DateTime.Now.Date || t.FechaLimite.Date < DateTime.Now.Date)
                .OrderBy(t => t.FechaLimite)
                .ToListAsync();

            // Usa:
            var entregasList = await _context.Entregas
                .Where(e => e.EstudianteId == userId)
                .Select(e => e.TareaId)
                .ToListAsync();
            var entregasRealizadas = entregasList.ToHashSet();

            var tareasViewModel = tareas.Select(t => new TareaEstudianteViewModel
            {
                Id = t.Id,
                Titulo = t.Titulo,
                Descripcion = t.Descripcion.Length > 100
                    ? t.Descripcion[..100] + "..."
                    : t.Descripcion,
                FechaPublicacion = t.FechaPublicacion,
                FechaLimite = t.FechaLimite,
                InstruccionesAdicionales = t.InstruccionesAdicionales,
                ColorSemaforo = t.ColorSemaforo,
                Entregada = entregasRealizadas.Contains(t.Id)
            }).ToList();

            return View(tareasViewModel);
        }

        // GET: Tareas/MisTareas (Estudiante - ver sus entregas)
        [Authorize(Roles = "Estudiante")]
        public async Task<IActionResult> MisTareas()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var entregas = await _context.Entregas
                .Include(e => e.Tarea)
                .Where(e => e.EstudianteId == userId)
                .OrderByDescending(e => e.FechaEntrega)
                .ToListAsync();

            return View(entregas);
        }

        // GET: Tareas/Crear (Docente)
        [Authorize(Roles = "Docente")]
        public IActionResult Crear()
        {
            return View(new TareaViewModel());
        }

        // POST: Tareas/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Docente")]
        public async Task<IActionResult> Crear(TareaViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var docenteId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var tarea = new Tarea
                {
                    Titulo = model.Titulo.Trim(),
                    Descripcion = model.Descripcion.Trim(),
                    FechaPublicacion = DateTime.Now,
                    FechaLimite = model.FechaLimite,
                    InstruccionesAdicionales = model.InstruccionesAdicionales?.Trim(),
                    Curso = model.Curso?.Trim(),
                    DocenteId = docenteId ?? string.Empty
                };

                _context.Tareas.Add(tarea);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Tarea publicada exitosamente";
                return RedirectToAction("Dashboard", "Inicio");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Error al publicar la tarea: " + ex.Message);
                return View(model);
            }
        }

        // GET: Tareas/Detalles/5
        public async Task<IActionResult> Detalles(int id)
        {
            var tarea = await _context.Tareas
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tarea == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var esDocente = User.IsInRole("Docente");

            // Si es estudiante, verificar si ya entregó
            if (!esDocente)
            {
                var entrega = await _context.Entregas
                    .FirstOrDefaultAsync(e => e.TareaId == id && e.EstudianteId == userId);

                ViewBag.YaEntrego = entrega != null;
                ViewBag.EntregaId = entrega?.Id;
                ViewBag.Calificacion = entrega?.Calificacion;
            }

            return View(tarea);
        }

        // GET: Tareas/Editar/5 (Docente)
        [Authorize(Roles = "Docente")]
        public async Task<IActionResult> Editar(int id)
        {
            var tarea = await _context.Tareas.FindAsync(id);
            if (tarea == null)
            {
                return NotFound();
            }

            var docenteId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (tarea.DocenteId != docenteId)
            {
                return Forbid();
            }

            var model = new TareaViewModel
            {
                Id = tarea.Id,
                Titulo = tarea.Titulo,
                Descripcion = tarea.Descripcion,
                FechaLimite = tarea.FechaLimite,
                InstruccionesAdicionales = tarea.InstruccionesAdicionales,
                Curso = tarea.Curso
            };

            return View(model);
        }

        // POST: Tareas/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Docente")]
        public async Task<IActionResult> Editar(int id, TareaViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var tarea = await _context.Tareas.FindAsync(id);
                if (tarea == null)
                {
                    return NotFound();
                }

                var docenteId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (tarea.DocenteId != docenteId)
                {
                    return Forbid();
                }

                tarea.Titulo = model.Titulo.Trim();
                tarea.Descripcion = model.Descripcion.Trim();
                tarea.FechaLimite = model.FechaLimite;
                tarea.InstruccionesAdicionales = model.InstruccionesAdicionales?.Trim();
                tarea.Curso = model.Curso?.Trim();

                _context.Update(tarea);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Tarea actualizada exitosamente";
                return RedirectToAction("Dashboard", "Inicio");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Tareas.Any(t => t.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        // POST: Tareas/Eliminar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Docente")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var tarea = await _context.Tareas
                .Include(t => t.Entregas)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tarea == null)
            {
                return Json(new { success = false, message = "Tarea no encontrada" });
            }

            var docenteId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (tarea.DocenteId != docenteId)
            {
                return Json(new { success = false, message = "No autorizado" });
            }

            try
            {
                // Eliminar archivos de entregas si existen
                foreach (var entrega in tarea.Entregas)
                {
                    if (!string.IsNullOrEmpty(entrega.RutaArchivo))
                    {
                        var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, entrega.RutaArchivo.TrimStart('/'));
                        if (System.IO.File.Exists(fullPath))
                        {
                            System.IO.File.Delete(fullPath);
                        }
                    }
                }

                _context.Tareas.Remove(tarea);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Tarea eliminada correctamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al eliminar: " + ex.Message });
            }
        }
    }
}