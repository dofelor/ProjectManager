using ProjectManager.Data;
using ProjectManager.Data.Entities;
using Microsoft.EntityFrameworkCore;
using ProjectManager.Logic.Dtos;


namespace ProjectManager.Logic.Services
{
    public class ProjectService : IProjectService
    {
        private readonly ApplicationDbContext _context;

        public ProjectService(ApplicationDbContext context)
        {
            _context = context; 
        }

        public async Task<List<ProjectDTO>> GetAllProjectAsync(int? priority = null, DateTime? from = null, DateTime? to = null, string? sortBy = null)
        {
            var query = _context.Projects
                .Include(p => p.Supervisor)
                .AsQueryable();

            // 1. Фильтрация
            if (priority.HasValue) query = query.Where(p => p.Priority == priority.Value);
            if (from.HasValue) query = query.Where(p => p.StartDate >= from.Value);
            if (to.HasValue) query = query.Where(p => p.StartDate <= to.Value);

            // 2. Сортировка
            query = sortBy switch
            {
                "Name" => query.OrderBy(p => p.Name),
                "StartDate" => query.OrderBy(p => p.StartDate),
                "Priority" => query.OrderByDescending(p => p.Priority),
                _ => query.OrderByDescending(p => p.Id)
            };

            // 3. Маппинг в DTO
            return await query.Select(p => new ProjectDTO
            {
                Id = p.Id,
                Name = p.Name,
                CustomerCompany = p.CustomerCompany,
                PerformingCompany = p.PerformingCompany,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Priority = p.Priority,
                SupervisorId = p.SupervisorId,
                SupervisorFullName = p.Supervisor != null
                    ? $"{p.Supervisor.LastName} {p.Supervisor.FirstName}"
                    : "Нет руководителя"
            }).ToListAsync();
        }

        public async Task CreateProjectAsync(ProjectDTO dto, List<int> employeeIds)
        {
            // Превращаем DTO в Entity
            var project = new Project
            {
                Name = dto.Name,
                CustomerCompany = dto.CustomerCompany,
                PerformingCompany = dto.PerformingCompany,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Priority = dto.Priority,
                SupervisorId = dto.SupervisorId
            };



            // Привязываем команду
            if (employeeIds != null && employeeIds.Any())
            {
                project.Employees = await _context.Employees
                    .Where(e => employeeIds.Contains(e.Id))
                    .ToListAsync();
            }

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveEmployeeFromProjectAsync(int projectId, int employeeId)
        {
            var project = await _context.Projects
                .Include(p => p.Employees) // Загружаем связанные данные
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project != null)
            {
                var emp = project.Employees.FirstOrDefault(e => e.Id == employeeId);
                if (emp != null)
                {
                    project.Employees.Remove(emp); // Удаляем связь из промежуточной таблицы
                    await _context.SaveChangesAsync(); // Сохраняем изменения
                }
            }
        }

        public async Task UpdateProjectAsync(int id, ProjectDTO updatedProject, List<int> employeeIds)
        {
            var existingProject = await _context.Projects
                .Include(p => p.Employees)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (existingProject == null) return;
            if (updatedProject.EndDate < updatedProject.StartDate)
            {
                throw new Exception("Дата окончания не может быть раньше даты начала!");
            }

            // Обновляем поля
            existingProject.Name = updatedProject.Name;
            existingProject.CustomerCompany = updatedProject.CustomerCompany;
            existingProject.PerformingCompany = updatedProject.PerformingCompany;
            existingProject.StartDate = updatedProject.StartDate;
            existingProject.EndDate = updatedProject.EndDate;
            existingProject.Priority = updatedProject.Priority;
            existingProject.SupervisorId = updatedProject.SupervisorId;

            // Обновляем команду (без двойного SaveChanges)
            existingProject.Employees.Clear();

            if (employeeIds != null && employeeIds.Any())
            {
                var members = await _context.Employees
                    .Where(e => employeeIds.Contains(e.Id))
                    .ToListAsync();

                foreach (var member in members)
                {
                    existingProject.Employees.Add(member);
                }
            }

            await _context.SaveChangesAsync(); // Сохраняем всё одним махом
        }

        public async Task<bool> DeleteProjectAsync(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null) return false;

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ProjectDTO?> GetProjectByIdAsync(int id)
        {
            var p = await _context.Projects
                .Include(p => p.Supervisor)
                .Include(p => p.Employees)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (p == null) return null;

            return new ProjectDTO
            {
                Id = p.Id,
                Name = p.Name,
                CustomerCompany = p.CustomerCompany,
                PerformingCompany = p.PerformingCompany,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Priority = p.Priority,
                SupervisorId = p.SupervisorId,
                Employees = p.Employees.Select(e => new EmployeeDTO
                {
                    Id = e.Id,
                    FirstName = e.FirstName,
                    LastName = e.LastName
                }).ToList()
            };
        }
           


        // Добавь в ProjectService.cs

        public async Task AddEmployeeToProjectAsync(int projectId, int employeeId)
        {
            var project = await _context.Projects
                .Include(p => p.Employees)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            var employee = await _context.Employees.FindAsync(employeeId);

            if (project != null && employee != null)
            {
                if (!project.Employees.Any(e => e.Id == employeeId))
                {
                    project.Employees.Add(employee);
                    await _context.SaveChangesAsync();
                }
            }
        }

    }
}
