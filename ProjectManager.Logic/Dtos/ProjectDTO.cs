using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectManager.Logic.Dtos
{
    public class ProjectDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string CustomerCompany { get; set; }
        public string PerformingCompany { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Priority { get; set; }

        // Данные руководителя
        public int SupervisorId { get; set; }
        public string? SupervisorFullName { get; set; }

        // Список ID и имен сотрудников для отображения
        public List<EmployeeDTO> Employees { get; set; } = new();
    }
}
