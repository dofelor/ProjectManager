using ProjectManager.Logic.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectManager.Logic.Services
{
    public interface IProjectTaskService
    {
        Task<IEnumerable<ProjectTaskDTO>> GetAllTasksAsync(int? projectId = null, int? status = null);
        Task<ProjectTaskDTO?> GetTaskByIdAsync(int id);
        Task CreateTaskAsync(ProjectTaskDTO taskDto);
        Task UpdateTaskAsync(ProjectTaskDTO taskDto);
        Task DeleteTaskAsync(int id);
    }
}
