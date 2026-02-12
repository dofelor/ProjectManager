using System.ComponentModel.DataAnnotations;

namespace ProjectManager.Web.Models
{
    public class CreateEmployeeViewModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; } // Пароль, который задаст Админ

        [Required(ErrorMessage = "Выберите роль")]
        public string Role { get; set; } // "Employee" или "ProjectManager"
    }
}
