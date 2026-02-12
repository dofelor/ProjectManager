using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectManager.Logic.Dtos
{
    public class EmployeeDTO
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? MiddleName { get; set; }
        public string Email { get; set; }
        // Вспомогательное свойство для отображения в списках
        public string FullName => $"{LastName} {FirstName} {MiddleName}".Trim();
    }
}
