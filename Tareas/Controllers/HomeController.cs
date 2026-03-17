using Microsoft.AspNetCore.Mvc;

namespace Tareas.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // Redirige a la página de Login en lugar del Dashboard
            return RedirectToPage("/Account/Login", new { area = "Identity" });
        }
    }
}