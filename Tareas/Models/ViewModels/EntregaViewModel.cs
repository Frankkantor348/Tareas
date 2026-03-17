using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Tareas.Models.ViewModels
{
    /// <summary>
    /// ViewModel para la entrega de tarea (estudiante)
    /// </summary>
    public class EntregaViewModel
    {
        [Required]
        public int TareaId { get; set; }

        [Display(Name = "Comentario adicional (opcional)")]
        [StringLength(500, ErrorMessage = "El comentario no puede exceder 500 caracteres")]
        public string? ComentarioEstudiante { get; set; }

        [Required(ErrorMessage = "Debes seleccionar un archivo para entregar")]
        [Display(Name = "Archivo de la tarea")]
        public IFormFile ArchivoEntrega { get; set; } = null!;
    }

    /// <summary>
    /// ViewModel para calificar una entrega (docente)
    /// </summary>
    public class CalificarEntregaViewModel
    {
        public int EntregaId { get; set; }
        public int TareaId { get; set; }
        public string TituloTarea { get; set; } = string.Empty;
        public string EstudianteId { get; set; } = string.Empty;
        public string NombreEstudiante { get; set; } = string.Empty;
        public DateTime FechaEntrega { get; set; }
        public string? NombreArchivo { get; set; }

        [Required(ErrorMessage = "La calificación es obligatoria")]
        [Range(0, 100, ErrorMessage = "La calificación debe estar entre 0 y 100")]
        [Display(Name = "Calificación (0-100)")]
        public decimal Calificacion { get; set; }

        [Display(Name = "Retroalimentación para el estudiante")]
        [StringLength(1000, ErrorMessage = "La retroalimentación no puede exceder 1000 caracteres")]
        public string? RetroalimentacionDocente { get; set; }
    }
}
