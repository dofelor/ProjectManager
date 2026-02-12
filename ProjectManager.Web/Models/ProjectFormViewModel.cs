using ProjectManager.Logic.Dtos;

namespace ProjectManager.Web.Models
{
    public class ProjectFormViewModel
    {
        public ProjectDTO Project { get; set; } = new();

        // Список всех доступных сотрудников для Dropdown
        public List<EmployeeDTO> AllEmployees { get; set; } = new();

        // Выбранные ID сотрудников (для шага 4 визарда)
        public List<int> SelectedEmployeeIds { get; set; } = new();
    }
}
