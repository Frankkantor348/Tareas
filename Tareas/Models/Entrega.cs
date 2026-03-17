using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tareas.Models
{
    /// <summary>
    /// Representa la entrega de una tarea por parte de un estudiante
    /// </summary>
    public class Entrega
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TareaId { get; set; }

        [Required]
        [Display(Name = "ID del estudiante")]
        public string EstudianteId { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Fecha de entrega")]
        public DateTime FechaEntrega { get; set; } = DateTime.Now;

        [Display(Name = "Comentario del estudiante")]
        [StringLength(500, ErrorMessage = "El comentario no puede exceder 500 caracteres")]
        public string? ComentarioEstudiante { get; set; }

        [Display(Name = "Archivo entregado")]
        [StringLength(255)]
        public string? RutaArchivo { get; set; }

        [Display(Name = "Nombre del archivo original")]
        [StringLength(255)]
        public string? NombreArchivoOriginal { get; set; }

        [Display(Name = "Calificación")]
        [Range(0, 100, ErrorMessage = "La calificación debe estar entre 0 y 100")]
        [Column(TypeName = "decimal(5,2)")]  // ← Agrega esta línea
        public decimal? Calificacion { get; set; }

        [Display(Name = "Retroalimentación del docente")]
        [StringLength(1000, ErrorMessage = "La retroalimentación no puede exceder 1000 caracteres")]
        public string? RetroalimentacionDocente { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Fecha de calificación")]
        public DateTime? FechaCalificacion { get; set; }

        // Propiedad calculada para verificar si está calificada
        [Display(Name = "Estado")]
        public string Estado => Calificacion.HasValue ? "Calificada" : "Pendiente de revisión";

        // Propiedades de navegación
        [ForeignKey("TareaId")]
        public virtual Tarea? Tarea { get; set; }
    }
}