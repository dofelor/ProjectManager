using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProjectManager.Logic.Dtos;
using ProjectManager.Logic.Services;
using ProjectManager.Web.Models;

namespace ProjectManager.Web.Controllers
{
    [Authorize(Roles = "Admin,ProjectManager")]
    public class EmployeesController : Controller
    {

        private readonly IEmployeeService _employeeService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<EmployeesController> _logger;

        public EmployeesController(IEmployeeService employeeService, 
            UserManager<IdentityUser> userManager,
            ILogger<EmployeesController> logger)
        {
            _employeeService = employeeService;
            _userManager = userManager;
            _logger = logger;
        }
        public async Task<IActionResult> Index()
        {
            var employees = await _employeeService.GetAllEmployeeAsync();
            return View(employees);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Create() { return View(); }


        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateEmployeeViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // 1. Создаем Identity аккаунт
            var user = new IdentityUser { UserName = model.Email, Email = model.Email, EmailConfirmed = true };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // 2. Назначаем роль
                await _userManager.AddToRoleAsync(user, model.Role);

                // 3. Создаем запись сотрудника через сервис
                var employeeDto = new EmployeeDTO
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    UserId = user.Id // Связь установлена сразу!
                };

                await _employeeService.AddEmployeeAsync(employeeDto);

                return RedirectToAction(nameof(Index));
            }

            // Если Identity не смог создать юзера (пароль слабый и т.д.)
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Search(string term)
        {
            var results = await _employeeService.SearchEmployeeAsync(term);
            return Json(results);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var employee = await _employeeService.GetEmployeeByIdAsync(id);
            if (employee == null) return NotFound();

            return View(employee);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EmployeeDTO model)
        {
            if (id != model.Id) return BadRequest();

            if (ModelState.IsValid)
            {
                await _employeeService.UpdateEmployeeAsync(id, model);
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // --- Методы для Удаления ---
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            // 1. Сначала находим сотрудника, чтобы забрать его Email
            var employee = await _employeeService.GetEmployeeByIdAsync(id);
            if (employee == null) return NotFound();

            var userEmail = employee.Email; // Запоминаем почту

            // 2. Удаляем из таблицы Employees
            var success = await _employeeService.DeleteEmployeeAsync(id);

            if (success)
            {
                // 3. ТЕПЕРЬ САМОЕ ВАЖНОЕ: Ищем юзера в AspNetUsers именно по Email
                // Это сработает, даже если связь по UserId была кривая
                var user = await _userManager.FindByEmailAsync(userEmail);

                if (user != null)
                {
                    var result = await _userManager.DeleteAsync(user);
                    if (!result.Succeeded)
                    {
                        // Если не удалилось, выведем ошибки Identity
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        _logger.LogError($"Не удалось удалить аккаунт Identity: {errors}");
                        TempData["Error"] = $"Профиль удален, но аккаунт в системе остался: {errors}";
                    }
                }
                else
                {
                    _logger.LogWarning($"Аккаунт с почтой {userEmail} не найден в AspNetUsers.");
                }

                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Удаление запрещено: сотрудник связан с задачами или проектами.";
            return RedirectToAction(nameof(Index));
        }
    }
}
