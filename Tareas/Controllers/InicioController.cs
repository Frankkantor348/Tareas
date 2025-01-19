using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.Linq;  // Para usar Any()
using System.Threading.Tasks;
using Tareas.Data;
using Tareas.Models;

namespace Tareas.Controllers
{
    public class InicioController : Controller
    {
        private readonly ApplicationDbContext _contexto;

        public InicioController(ApplicationDbContext contexto)
        {
            _contexto = contexto;
        }

        // Acción GET para la lista de tareas
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var tareas = await _contexto.Tarea.ToListAsync();
            return View(tareas);
        }

        // Acción GET para la creación de una nueva tarea
        [HttpGet]
        public IActionResult Crear()
        {
            return View();
        }

        // Acción POST para procesar la creación de una nueva tarea
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Tarea tarea)
        {
            if (!ModelState.IsValid)
            {
                return View(tarea);  // Regresar el formulario con los errores de validación
            }

            try
            {
                _contexto.Tarea.Add(tarea);
                await _contexto.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar la tarea.");
                return View(tarea);
            }
        }

        // Acción GET para ver los detalles de una tarea
        [HttpGet]
        public async Task<IActionResult> Detalles(int id)
        {
            var tarea = await _contexto.Tarea.FindAsync(id);
            if (tarea == null)
            {
                return NotFound();  // Si no se encuentra la tarea, devolver error 404
            }
            return View(tarea);  // Devolvemos la vista con la tarea
        }


        // Acción GET para editar una tarea
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var tarea = await _contexto.Tarea.FindAsync(id);
            if (tarea == null)
            {
                return NotFound();  // Si no se encuentra la tarea, devolver error 404
            }
            return View(tarea);
        }

        // Acción POST para procesar la edición de una tarea
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Tarea tarea)
        {
            if (id != tarea.Id)
            {
                return NotFound();  // Si el ID no coincide, devolver error 404
            }

            if (!ModelState.IsValid)
            {
                return View(tarea);  // Si no es válido, regresar al formulario de edición
            }

            try
            {
                _contexto.Update(tarea);  // Actualizamos la tarea
                await _contexto.SaveChangesAsync();
                return RedirectToAction(nameof(Index));  // Redirigimos al índice
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_contexto.Tarea.Any(t => t.Id == id))
                {
                    return NotFound();  // Si la tarea no existe, devolver error 404
                }
                else
                {
                    throw;  // Propagamos cualquier otro error
                }
            }
        }

        // Acción GET para eliminar una tarea
        [HttpGet]
        public async Task<IActionResult> Eliminar(int id)
        {
            var tarea = await _contexto.Tarea.FindAsync(id);
            if (tarea == null)
            {
                return NotFound();  // Si no se encuentra la tarea, devolver error 404
            }
            return View(tarea);
        }

        // Acción POST para procesar la eliminación de una tarea
        [HttpPost, ActionName("Eliminar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarConfirmado(int id)
        {
            var tarea = await _contexto.Tarea.FindAsync(id);
            if (tarea == null)
            {
                return NotFound();  // Si no se encuentra la tarea, devolver error 404
            }

            _contexto.Tarea.Remove(tarea);  // Eliminamos la tarea
            await _contexto.SaveChangesAsync();
            return RedirectToAction(nameof(Index));  // Redirigimos al índice
        }

        // Acción para la privacidad (de ejemplo)
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
}





