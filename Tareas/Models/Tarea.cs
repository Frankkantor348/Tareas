using System;
using System.ComponentModel.DataAnnotations;

namespace Tareas.Models
{
    public class Tarea
    {
        [Key]
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Ingrese el Nombre de Tarea")]
        public string? Nombre { get; set; }
        
        [Required(ErrorMessage = "Ingrese una Descripción")]
        public string? Descripcion { get; set; }
        
        [DataType(DataType.Date)]
        public DateTime FechaCreacion { get; set; }
        
        [DataType(DataType.Date)]
        public DateTime? FechaVencimiento { get; set; }
        
        [Required(ErrorMessage = "Seleccione un estado")]
        public EstadoTarea Estado { get; set; } = EstadoTarea.Pendiente;
        
        [Required(ErrorMessage = "Seleccione una prioridad")]
        public PrioridadTarea Prioridad { get; set; } = PrioridadTarea.Media;
    }

    public enum EstadoTarea
    {
        Pendiente = 1,
        EnProgreso = 2,
        Completada = 3
    }

    public enum PrioridadTarea
    {
        Baja = 1,
        Media = 2,
        Alta = 3
    }
}
