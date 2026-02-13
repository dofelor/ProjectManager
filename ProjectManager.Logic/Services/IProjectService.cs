using ProjectManager.Data.Entities;
using ProjectManager.Logic.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProjectManager.Logic.Services
{
    public interface IProjectService
    {
        // Sorting and single project retrieval added
        Task<List<ProjectDTO>> GetAllProjectAsync(int? priority = null, DateTime? from = null, DateTime? to = null, string? sortBy = null);
        Task<ProjectDTO?> GetProjectByIdAsync(int id);

        // employeeIds — list of IDs for many-to-many relationship
        Task CreateProjectAsync(ProjectDTO project, List<int> employeeIds);
        Task UpdateProjectAsync(int id, ProjectDTO project, List<int> employeeIds);

        Task<bool> DeleteProjectAsync(int id);

        // Team management (Functional Requirement)
        Task AddEmployeeToProjectAsync(int projectId, int employeeId);
        Task RemoveEmployeeFromProjectAsync(int projectId, int employeeId);
    }
}
