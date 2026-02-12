using Microsoft.EntityFrameworkCore;
using ProjectManager.Data;
using ProjectManager.Data.Entities;
using ProjectManager.Logic.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProjectManager.Logic.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly ApplicationDbContext _context;
        public EmployeeService(ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task<List<EmployeeDTO>> GetAllEmployeeAsync()
        {
            return await _context.Employees
                .Select(e => new EmployeeDTO
                {
                    Id = e.Id,
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    MiddleName = e.MiddleName,
                    Email = e.Email
                })
                .ToListAsync();
        }


        public async Task AddEmployeeAsync(EmployeeDTO dto)
        {
            var employee = new Employee
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                MiddleName = dto.MiddleName,
                Email = dto.Email
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
        }

        public async Task<List<EmployeeDTO>> SearchEmployeeAsync(string term)
        {
            var query = _context.Employees.AsQueryable();

            if (!string.IsNullOrWhiteSpace(term))
            {
                query = query.Where(e => e.LastName.Contains(term) || e.FirstName.Contains(term));
            }

            return await query
                .Select(e => new EmployeeDTO
                {
                    Id = e.Id,
                    FirstName = e.FirstName,
                    LastName = e.LastName,
                    MiddleName = e.MiddleName,
                    Email = e.Email
                })
                .ToListAsync();
        }

        public async Task UpdateEmployeeAsync(int id, EmployeeDTO updatedData)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return;

            employee.FirstName = updatedData.FirstName;
            employee.LastName = updatedData.LastName;
            employee.MiddleName = updatedData.MiddleName;
            employee.Email = updatedData.Email;

            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            // 1. Проверяем, не является ли он супервизором любого проекта
            bool isSupervisor = await _context.Projects.AnyAsync(p => p.SupervisorId == id);
            if (isSupervisor) return false;

            var employee = await _context.Employees
                .Include(e => e.Projects) // Чтобы очистить связи many-to-many
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee != null)
            {
                // Очищаем связи с проектами, где он просто исполнитель
                employee.Projects?.Clear();
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }


        public async Task<EmployeeDTO?> GetEmployeeByIdAsync(int id)
        {
            var e = await _context.Employees.FindAsync(id);
            if (e == null) return null;

            return new EmployeeDTO
            {
                Id = e.Id,
                FirstName = e.FirstName,
                LastName = e.LastName,
                MiddleName = e.MiddleName,
                Email = e.Email
            };
        }

    }
}
