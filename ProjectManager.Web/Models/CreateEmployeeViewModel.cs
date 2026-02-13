using System.ComponentModel.DataAnnotations;

namespace ProjectManager.Web.Models
{
    public class CreateEmployeeViewModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; } // Password set by Admin

        [Required(ErrorMessage = "Select a role")]
        public string Role { get; set; } // "Employee" or "ProjectManager"
    }
}
