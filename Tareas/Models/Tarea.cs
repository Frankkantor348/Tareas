using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tareas.Models
{
    /// <summary>
    /// Representa una tarea académica publicada por un docente
    /// </summary>
    public class Tarea
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El título de la tarea es obligatorio")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "El título debe tener entre 5 y 200 caracteres")]
        [Display(Name = "Título de la tarea")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descripción es obligatoria")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "La descripción debe tener entre 10 y 2000 caracteres")]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha de publicación es obligatoria")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de publicación")]
        public DateTime FechaPublicacion { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "La fecha límite de entrega es obligatoria")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha límite de entrega")]
        [FechaVencimientoMayorAHoy(ErrorMessage = "La fecha límite debe ser mayor o igual a la fecha actual")]
        public DateTime FechaLimite { get; set; }

        [Display(Name = "Instrucciones adicionales")]
        [StringLength(1000, ErrorMessage = "Las instrucciones no pueden exceder 1000 caracteres")]
        public string? InstruccionesAdicionales { get; set; }

        [Required]
        [Display(Name = "ID del docente")]
        public string DocenteId { get; set; } = string.Empty;

        [Display(Name = "Curso/Grado")]
        [StringLength(50, ErrorMessage = "El curso no puede exceder 50 caracteres")]
        public string? Curso { get; set; }

        // Propiedad calculada para el semáforo
        [Display(Name = "Estado de urgencia")]
        public string ColorSemaforo
        {
            get
            {
                var diasRestantes = (FechaLimite - DateTime.Now).Days;
                return diasRestantes switch
                {
                    < 0 => "rojo",        // Vencida
                    <= 3 => "amarillo",   // Próxima a vencer
                    _ => "verde"          // Con tiempo suficiente
                };
            }
        }

        // Propiedad de navegación
        public ICollection<Entrega> Entregas { get; set; } = new List<Entrega>();
    }

    /// <summary>
    /// Atributo de validación personalizado para asegurar que la fecha límite sea mayor o igual a la actual
    /// </summary>
    public class FechaVencimientoMayorAHoyAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is DateTime fecha)
            {
                if (fecha.Date < DateTime.Now.Date)
                {
                    return new ValidationResult(ErrorMessage ?? "La fecha debe ser hoy o futura");
                }
            }
            return ValidationResult.Success;
        }
    }
}