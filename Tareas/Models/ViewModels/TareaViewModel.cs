using System.ComponentModel.DataAnnotations;

namespace Tareas.Models.ViewModels
{
    /// <summary>
    /// ViewModel para la creación/edición de tareas (docente)
    /// </summary>
    public class TareaViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "El título de la tarea es obligatorio")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "El título debe tener entre 5 y 200 caracteres")]
        [Display(Name = "Título de la tarea")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descripción es obligatoria")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "La descripción debe tener entre 10 y 2000 caracteres")]
        [Display(Name = "Descripción detallada")]
        public string Descripcion { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha límite es obligatoria")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha límite de entrega")]
        public DateTime FechaLimite { get; set; } = DateTime.Now.AddDays(7);

        [Display(Name = "Instrucciones adicionales")]
        [DataType(DataType.MultilineText)]
        public string? InstruccionesAdicionales { get; set; }

        [Display(Name = "Curso/Grado")]
        [StringLength(50)]
        public string? Curso { get; set; }
    }

    /// <summary>
    /// ViewModel para la visualización de tareas por parte del estudiante
    /// </summary>
    public class TareaEstudianteViewModel
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public DateTime FechaPublicacion { get; set; }
        public DateTime FechaLimite { get; set; }
        public string? InstruccionesAdicionales { get; set; }
        public string ColorSemaforo { get; set; } = string.Empty;
        public bool Entregada { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public bool Calificada { get; set; }
        public decimal? Calificacion { get; set; }
    }

    /// <summary>
    /// ViewModel para el dashboard del estudiante
    /// </summary>
    public class DashboardEstudianteViewModel
    {
        public int TareasPendientes { get; set; }
        public int TareasEntregadas { get; set; }
        public int TareasCalificadas { get; set; }
        public int TareasVencidas { get; set; }
        public List<TareaEstudianteViewModel> TareasProximas { get; set; } = new();
    }

    /// <summary>
    /// ViewModel para el dashboard del docente
    /// </summary>
    public class DashboardDocenteViewModel
    {
        public int TotalTareasPublicadas { get; set; }
        public int TotalEntregasPendientes { get; set; }
        public int TotalEstudiantes { get; set; }
        public List<TareaResumenViewModel> TareasRecientes { get; set; } = new();
    }

    public class TareaResumenViewModel
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public DateTime FechaLimite { get; set; }
        public int EntregasRealizadas { get; set; }
        public int EntregasPendientes { get; set; }
    }
}
