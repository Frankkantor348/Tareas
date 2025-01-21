using System;
using System.ComponentModel.DataAnnotations;

namespace Tareas.Models
{
    public class Tarea
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage ="Ingrese el Nombre de Tarea")]
        public string? Nombre { get; set; }
        [Required(ErrorMessage ="Ingrese una Descripción")]
        public string? Descripcion { get; set; }
        
        [DataType(DataType.Date)]
        public DateTime FechaCreacion { get; set; }
    }
}
