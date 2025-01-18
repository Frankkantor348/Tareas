﻿using System.ComponentModel.DataAnnotations;

namespace Tareas.Models
{
    public class Tarea
    {
        [Key]
        public int Id { get; set; }
      
        public string? Nombre { get; set; }
      
        public string? Descripcion { get; set; }
    }
}
