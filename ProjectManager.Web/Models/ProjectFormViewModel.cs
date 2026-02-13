using ProjectManager.Logic.Dtos;

namespace ProjectManager.Web.Models
{
    public class ProjectFormViewModel
    {
        public ProjectDTO Project { get; set; } = new();

        // List of all available employees for Dropdown
        public List<EmployeeDTO> AllEmployees { get; set; } = new();

        // Selected employee IDs (for wizard step 4)
        public List<int> SelectedEmployeeIds { get; set; } = new();
    }
}
