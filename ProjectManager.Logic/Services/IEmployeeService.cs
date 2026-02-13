using ProjectManager.Data;
using ProjectManager.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using ProjectManager.Logic.Dtos;

namespace ProjectManager.Logic.Services
{
    public interface IEmployeeService
    {
        Task<List<EmployeeDTO>> GetAllEmployeeAsync();
        Task<EmployeeDTO?> GetEmployeeByIdAsync(int id); // Needed for "Edit" View
        Task<List<EmployeeDTO>> SearchEmployeeAsync(string term); // That AJAX search
        Task AddEmployeeAsync(EmployeeDTO employee);
        Task UpdateEmployeeAsync(int id, EmployeeDTO employee); // Usually entity/DTO itself is passed
        Task<bool> DeleteEmployeeAsync(int id);
    }
}
