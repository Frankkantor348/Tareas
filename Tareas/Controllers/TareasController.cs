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
    // InstruccionesAdicionales = t.InstruccionesAdicionales, // ← Comentado si no existe en el ViewModel
    ColorSemaforo = t.ColorSemaforo,
    Entregada = entregasRealizadas.Contains(t.Id),
    RutaArchivoApoyo = t.RutaArchivoApoyo,
    NombreArchivoApoyo = t.NombreArchivoApoyo,
    TipoArchivoApoyo = t.TipoArchivoApoyo
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

        // POST: Tareas/Crear (con soporte para archivo de apoyo)
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

                // PROCESAR ARCHIVO DE APOYO
                if (model.ArchivoApoyo != null && model.ArchivoApoyo.Length > 0)
                {
                    // Validar extensión
                    var extension = Path.GetExtension(model.ArchivoApoyo.FileName).ToLower();
                    var extensionesPermitidas = new[] { ".pdf", ".doc", ".docx", ".txt", ".ppt", ".pptx", ".xls", ".xlsx", ".jpg", ".png", ".zip" };

                    if (!extensionesPermitidas.Contains(extension))
                    {
                        ModelState.AddModelError("ArchivoApoyo", "Tipo de archivo no permitido");
                        return View(model);
                    }

                    // Validar tamaño (20MB max)
                    if (model.ArchivoApoyo.Length > 20 * 1024 * 1024)
                    {
                        ModelState.AddModelError("ArchivoApoyo", "El archivo no puede ser mayor a 20MB");
                        return View(model);
                    }

                    // Crear nombre único
                    var nombreArchivo = $"{Guid.NewGuid()}{extension}";
                    var carpeta = Path.Combine("uploads", "material-apoyo");
                    var rutaCarpeta = Path.Combine(_webHostEnvironment.WebRootPath, carpeta);

                    if (!Directory.Exists(rutaCarpeta))
                    {
                        Directory.CreateDirectory(rutaCarpeta);
                    }

                    var rutaCompleta = Path.Combine(rutaCarpeta, nombreArchivo);
                    using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                    {
                        await model.ArchivoApoyo.CopyToAsync(stream);
                    }

                    tarea.RutaArchivoApoyo = Path.Combine("/", carpeta, nombreArchivo).Replace("\\", "/");
                    tarea.NombreArchivoApoyo = model.ArchivoApoyo.FileName;
                    tarea.TipoArchivoApoyo = extension;
                    tarea.TamañoArchivoApoyo = model.ArchivoApoyo.Length;
                    tarea.FechaSubidaArchivo = DateTime.Now;
                }

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
                .Include(t => t.Entregas)
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
                Curso = tarea.Curso,
                // Cargar datos del archivo existente
                RutaArchivoApoyo = tarea.RutaArchivoApoyo,
                NombreArchivoApoyo = tarea.NombreArchivoApoyo,
                TipoArchivoApoyo = tarea.TipoArchivoApoyo,
                TamañoArchivoApoyo = tarea.TamañoArchivoApoyo
            };

            return View(model);
        }

        // POST: Tareas/Editar/5 (con soporte para archivo de apoyo)
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

                // PROCESAR NUEVO ARCHIVO DE APOYO (si se subió)
                if (model.ArchivoApoyo != null && model.ArchivoApoyo.Length > 0)
                {
                    // Validar extensión
                    var extension = Path.GetExtension(model.ArchivoApoyo.FileName).ToLower();
                    var extensionesPermitidas = new[] { ".pdf", ".doc", ".docx", ".txt", ".ppt", ".pptx", ".xls", ".xlsx", ".jpg", ".png", ".zip" };

                    if (!extensionesPermitidas.Contains(extension))
                    {
                        ModelState.AddModelError("ArchivoApoyo", "Tipo de archivo no permitido");
                        return View(model);
                    }

                    // Validar tamaño (20MB max)
                    if (model.ArchivoApoyo.Length > 20 * 1024 * 1024)
                    {
                        ModelState.AddModelError("ArchivoApoyo", "El archivo no puede ser mayor a 20MB");
                        return View(model);
                    }

                    // Eliminar archivo anterior si existe
                    if (!string.IsNullOrEmpty(tarea.RutaArchivoApoyo))
                    {
                        var rutaAnterior = Path.Combine(_webHostEnvironment.WebRootPath, tarea.RutaArchivoApoyo.TrimStart('/'));
                        if (System.IO.File.Exists(rutaAnterior))
                        {
                            System.IO.File.Delete(rutaAnterior);
                        }
                    }

                    // Guardar nuevo archivo
                    var nombreArchivo = $"{Guid.NewGuid()}{extension}";
                    var carpeta = Path.Combine("uploads", "material-apoyo");
                    var rutaCarpeta = Path.Combine(_webHostEnvironment.WebRootPath, carpeta);

                    if (!Directory.Exists(rutaCarpeta))
                    {
                        Directory.CreateDirectory(rutaCarpeta);
                    }

                    var rutaCompleta = Path.Combine(rutaCarpeta, nombreArchivo);
                    using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                    {
                        await model.ArchivoApoyo.CopyToAsync(stream);
                    }

                    tarea.RutaArchivoApoyo = Path.Combine("/", carpeta, nombreArchivo).Replace("\\", "/");
                    tarea.NombreArchivoApoyo = model.ArchivoApoyo.FileName;
                    tarea.TipoArchivoApoyo = extension;
                    tarea.TamañoArchivoApoyo = model.ArchivoApoyo.Length;
                    tarea.FechaSubidaArchivo = DateTime.Now;
                }

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

                // Eliminar archivo de apoyo de la tarea si existe
                if (!string.IsNullOrEmpty(tarea.RutaArchivoApoyo))
                {
                    var rutaArchivo = Path.Combine(_webHostEnvironment.WebRootPath, tarea.RutaArchivoApoyo.TrimStart('/'));
                    if (System.IO.File.Exists(rutaArchivo))
                    {
                        System.IO.File.Delete(rutaArchivo);
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