using Microsoft.EntityFrameworkCore;
using ProjectManager.Data;
using ProjectManager.Data.Entities;
using ProjectManager.Logic.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyTaskStatus = ProjectManager.Data.Entities.TaskStatus; // Добавь это

namespace ProjectManager.Logic.Services
{
    public class ProjectTaskService : IProjectTaskService
    {
        private readonly ApplicationDbContext _context;

        public ProjectTaskService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ProjectTaskDTO>> GetAllTasksAsync(int? projectId = null, int? status = null)
        {
            var query = _context.ProjectTasks
                .Include(t => t.Project)
                .Include(t => t.Author)
                .Include(t => t.Executor)
                .AsQueryable();

            // Фильтрация по ТЗ
            if (projectId.HasValue) query = query.Where(t => t.ProjectId == projectId);
            if (status.HasValue) query = query.Where(t => t.Status == (MyTaskStatus)status);

            return await query.Select(t => new ProjectTaskDTO
            {
                Id = t.Id,
                Name = t.Name,
                ProjectName = t.Project.Name,
                AuthorFullName = t.Author.FirstName + " " + t.Author.LastName,
                ExecutorFullName = t.Executor.FirstName + " " + t.Executor.LastName,
                ExecutorUserId = t.Executor.UserId,
                Status = (int)t.Status,
                Priority = t.Priority,
                Comment = t.Comment
            }).ToListAsync();
        }

        public async Task<ProjectTaskDTO?> GetTaskByIdAsync(int id)
        {
            var t = await _context.ProjectTasks
                .Include(t => t.Author)
                .Include(t => t.Executor)
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (t == null) return null;

            return new ProjectTaskDTO
            {
                Id = t.Id,
                Name = t.Name,
                ProjectId = t.ProjectId,
                ProjectName = t.Project?.Name,
                AuthorId = t.AuthorId,
                AuthorFullName = t.Author != null ? t.Author.FirstName + " " + t.Author.LastName : null,
                ExecutorId = t.ExecutorId,
                ExecutorFullName = t.Executor != null ? t.Executor.FirstName + " " + t.Executor.LastName : null,
                ExecutorUserId = t.Executor?.UserId,
                Status = (int)t.Status,
                Priority = t.Priority,
                Comment = t.Comment
            };
        }

        public async Task CreateTaskAsync(ProjectTaskDTO dto)
        {
            var task = new ProjectTask
            {
                Name = dto.Name,
                ProjectId = dto.ProjectId,
                AuthorId = dto.AuthorId,
                ExecutorId = dto.ExecutorId,
                Priority = dto.Priority,
                Status = (MyTaskStatus)dto.Status,
                Comment = dto.Comment
            };

            _context.ProjectTasks.Add(task);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateTaskAsync(ProjectTaskDTO dto)
        {
            var task = await _context.ProjectTasks.FindAsync(dto.Id);
            if (task == null) return;

            task.Name = dto.Name;
            task.ProjectId = dto.ProjectId;
            task.AuthorId = dto.AuthorId;
            task.ExecutorId = dto.ExecutorId;
            task.Priority = dto.Priority;
            task.Status = (MyTaskStatus)dto.Status; // Используем полное имя
            task.Comment = dto.Comment;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteTaskAsync(int id)
        {
            var task = await _context.ProjectTasks.FindAsync(id);
            if (task != null)
            {
                _context.ProjectTasks.Remove(task);
                await _context.SaveChangesAsync();
            }
        }
    }
}
