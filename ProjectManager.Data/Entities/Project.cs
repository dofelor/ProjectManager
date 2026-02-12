using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // Нужно добавить

namespace ProjectManager.Data.Entities
{
    public class Project
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)] // Ограничиваем длину названия проекта
        public string Name { get; set; }

        [Required]
        [MaxLength(255)]
        public string CustomerCompany { get; set; }

        [Required]
        [MaxLength(255)]
        public string PerformingCompany { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Range(0, 100)] // Приоритет обычно имеет разумные границы
        public int Priority { get; set; }

        // Связи
        [Required]
        public int SupervisorId { get; set; }
        public Employee Supervisor { get; set; }

        public List<Employee> Employees { get; set; } = new();
    }
}