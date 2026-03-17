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
    public class EntregasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<IdentityUser> _userManager;

        public EntregasController(
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
        }

        // GET: Entregas/Tarea/5 (Docente - lista entregas de una tarea)
        [Authorize(Roles = "Docente")]
        public async Task<IActionResult> Tarea(int id)
        {
            var tarea = await _context.Tareas
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tarea == null)
            {
                return NotFound();
            }

            var docenteId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (tarea.DocenteId != docenteId)
            {
                return Forbid();
            }

            var entregas = await _context.Entregas
                .Where(e => e.TareaId == id)
                .OrderBy(e => e.FechaEntrega)
                .ToListAsync();

            // Obtener nombres de estudiantes (en un sistema real, tendrías una tabla de perfiles)
            var estudiantesIds = entregas.Select(e => e.EstudianteId).Distinct().ToList();
            var estudiantes = await _userManager.Users
                .Where(u => estudiantesIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Email ?? "Desconocido");

            ViewBag.Tarea = tarea;
            ViewBag.Estudiantes = estudiantes;

            return View(entregas);
        }

        // GET: Entregas/Crear/5 (Estudiante - entregar tarea)
        [Authorize(Roles = "Estudiante")]
        public async Task<IActionResult> Crear(int id)
        {
            var tarea = await _context.Tareas.FindAsync(id);
            if (tarea == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Verificar si ya entregó
            var yaEntrego = await _context.Entregas
                .AnyAsync(e => e.TareaId == id && e.EstudianteId == userId);

            if (yaEntrego)
            {
                TempData["Warning"] = "Ya has entregado esta tarea anteriormente";
                return RedirectToAction("Detalles", "Tareas", new { id });
            }

            // Verificar si está vencida
            if (tarea.FechaLimite.Date < DateTime.Now.Date)
            {
                TempData["Error"] = "Esta tarea ya está vencida, no se puede entregar";
                return RedirectToAction("Detalles", "Tareas", new { id });
            }

            ViewBag.TareaTitulo = tarea.Titulo;
            return View(new EntregaViewModel { TareaId = id });
        }

        // POST: Entregas/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Estudiante")]
        public async Task<IActionResult> Crear(EntregaViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.TareaTitulo = _context.Tareas
                    .Where(t => t.Id == model.TareaId)
                    .Select(t => t.Titulo)
                    .FirstOrDefault();
                return View(model);
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var tarea = await _context.Tareas.FindAsync(model.TareaId);

                if (tarea == null)
                {
                    return NotFound();
                }

                // Verificar nuevamente si ya entregó (por si acaso)
                var yaEntrego = await _context.Entregas
                    .AnyAsync(e => e.TareaId == model.TareaId && e.EstudianteId == userId);

                if (yaEntrego)
                {
                    TempData["Error"] = "Ya has entregado esta tarea anteriormente";
                    return RedirectToAction("Detalles", "Tareas", new { id = model.TareaId });
                }

                // Guardar archivo
                string? rutaArchivo = null;
                string? nombreArchivo = null;

                if (model.ArchivoEntrega != null && model.ArchivoEntrega.Length > 0)
                {
                    // Validar extensión
                    var extension = Path.GetExtension(model.ArchivoEntrega.FileName).ToLower();
                    var extensionesPermitidas = new[] { ".pdf", ".doc", ".docx", ".txt", ".zip", ".jpg", ".png" };

                    if (!extensionesPermitidas.Contains(extension))
                    {
                        ModelState.AddModelError("ArchivoEntrega", "Tipo de archivo no permitido");
                        ViewBag.TareaTitulo = tarea.Titulo;
                        return View(model);
                    }

                    // Validar tamaño (10MB max)
                    if (model.ArchivoEntrega.Length > 10 * 1024 * 1024)
                    {
                        ModelState.AddModelError("ArchivoEntrega", "El archivo no puede ser mayor a 10MB");
                        ViewBag.TareaTitulo = tarea.Titulo;
                        return View(model);
                    }

                    // Crear nombre único
                    nombreArchivo = $"{Guid.NewGuid()}{extension}";
                    var carpeta = Path.Combine("uploads", "entregas", model.TareaId.ToString());
                    var rutaCarpeta = Path.Combine(_webHostEnvironment.WebRootPath, carpeta);

                    if (!Directory.Exists(rutaCarpeta))
                    {
                        Directory.CreateDirectory(rutaCarpeta);
                    }

                    var rutaCompleta = Path.Combine(rutaCarpeta, nombreArchivo);
                    using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                    {
                        await model.ArchivoEntrega.CopyToAsync(stream);
                    }

                    rutaArchivo = Path.Combine("/", carpeta, nombreArchivo).Replace("\\", "/");
                }

                var entrega = new Entrega
                {
                    TareaId = model.TareaId,
                    EstudianteId = userId,
                    FechaEntrega = DateTime.Now,
                    ComentarioEstudiante = model.ComentarioEstudiante,
                    RutaArchivo = rutaArchivo,
                    NombreArchivoOriginal = model.ArchivoEntrega?.FileName
                };

                _context.Entregas.Add(entrega);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Tarea entregada correctamente";
                return RedirectToAction("Detalles", "Tareas", new { id = model.TareaId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Error al entregar la tarea: " + ex.Message);
                ViewBag.TareaTitulo = _context.Tareas
                    .Where(t => t.Id == model.TareaId)
                    .Select(t => t.Titulo)
                    .FirstOrDefault();
                return View(model);
            }
        }

        // GET: Entregas/Calificar/5 (Docente)
        [Authorize(Roles = "Docente")]
        public async Task<IActionResult> Calificar(int id)
        {
            var entrega = await _context.Entregas
                .Include(e => e.Tarea)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (entrega == null)
            {
                return NotFound();
            }

            var docenteId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (entrega.Tarea?.DocenteId != docenteId)
            {
                return Forbid();
            }

            // Obtener email del estudiante (simplificado)
            var estudiante = await _userManager.FindByIdAsync(entrega.EstudianteId);

            var model = new CalificarEntregaViewModel
            {
                EntregaId = entrega.Id,
                TareaId = entrega.TareaId,
                TituloTarea = entrega.Tarea?.Titulo ?? string.Empty,
                EstudianteId = entrega.EstudianteId,
                NombreEstudiante = estudiante?.Email ?? "Desconocido",
                FechaEntrega = entrega.FechaEntrega,
                NombreArchivo = entrega.NombreArchivoOriginal,
                Calificacion = entrega.Calificacion ?? 0,
                RetroalimentacionDocente = entrega.RetroalimentacionDocente
            };

            return View(model);
        }

        // POST: Entregas/Calificar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Docente")]
        public async Task<IActionResult> Calificar(int id, CalificarEntregaViewModel model)
        {
            if (id != model.EntregaId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var entrega = await _context.Entregas
                    .Include(e => e.Tarea)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (entrega == null)
                {
                    return NotFound();
                }

                var docenteId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (entrega.Tarea?.DocenteId != docenteId)
                {
                    return Forbid();
                }

                entrega.Calificacion = model.Calificacion;
                entrega.RetroalimentacionDocente = model.RetroalimentacionDocente?.Trim();
                entrega.FechaCalificacion = DateTime.Now;

                _context.Update(entrega);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Calificación guardada correctamente";
                return RedirectToAction("Tarea", new { id = entrega.TareaId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Error al calificar: " + ex.Message);
                return View(model);
            }
        }

        // GET: Entregas/Descargar/5
        public async Task<IActionResult> Descargar(int id)
        {
            var entrega = await _context.Entregas
                .Include(e => e.Tarea)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (entrega == null || string.IsNullOrEmpty(entrega.RutaArchivo))
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var esDocente = User.IsInRole("Docente");
            var esPropietario = entrega.EstudianteId == userId;

            // Solo el dueño o el docente pueden descargar
            if (!esDocente && !esPropietario)
            {
                return Forbid();
            }

            // Si es docente, verificar que sea el docente de la tarea
            if (esDocente && entrega.Tarea?.DocenteId != userId)
            {
                return Forbid();
            }

            var rutaFisica = Path.Combine(_webHostEnvironment.WebRootPath, entrega.RutaArchivo.TrimStart('/'));
            if (!System.IO.File.Exists(rutaFisica))
            {
                return NotFound();
            }

            var nombreDescarga = $"{entrega.Tarea?.Titulo ?? "tarea"}_{entrega.NombreArchivoOriginal ?? "archivo"}";
            var bytes = await System.IO.File.ReadAllBytesAsync(rutaFisica);
            return File(bytes, "application/octet-stream", nombreDescarga);
        }
    }
}