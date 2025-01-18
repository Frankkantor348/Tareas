using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
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
                // Agregar la fecha de creación si está definida en el modelo
                //tarea.FechaCreacion = DateTime.Now;

                // Agregar la tarea a la base de datos
                _contexto.Tarea.Add(tarea);
                await _contexto.SaveChangesAsync();

                // Redirigir a la vista principal (índice)
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Aquí podrías registrar el error o mostrar un mensaje de error genérico
                ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar la tarea.");
                return View(tarea);
            }
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




