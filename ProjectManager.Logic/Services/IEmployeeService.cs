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
        Task<EmployeeDTO?> GetEmployeeByIdAsync(int id); // Нужно для View "Edit"
        Task<List<EmployeeDTO>> SearchEmployeeAsync(string term); // Тот самый AJAX-поиск
        Task AddEmployeeAsync(EmployeeDTO employee);
        Task UpdateEmployeeAsync(int id, EmployeeDTO employee); // Обычно передают саму сущность/DTO
        Task<bool> DeleteEmployeeAsync(int id);
    }
}
