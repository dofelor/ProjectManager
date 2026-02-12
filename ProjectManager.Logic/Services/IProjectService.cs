using ProjectManager.Data.Entities;
using ProjectManager.Logic.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProjectManager.Logic.Services
{
    public interface IProjectService
    {
        // Добавлена сортировка и получение одного проекта
        Task<List<ProjectDTO>> GetAllProjectAsync(int? priority = null, DateTime? from = null, DateTime? to = null, string? sortBy = null);
        Task<ProjectDTO?> GetProjectByIdAsync(int id);

        // employeeIds — список ID для связи many-to-many
        Task CreateProjectAsync(ProjectDTO project, List<int> employeeIds);
        Task UpdateProjectAsync(int id, ProjectDTO project, List<int> employeeIds);

        Task<bool> DeleteProjectAsync(int id);

        // Управление составом команды (Functional Requirement)
        Task AddEmployeeToProjectAsync(int projectId, int employeeId);
        Task RemoveEmployeeFromProjectAsync(int projectId, int employeeId);
    }
}
