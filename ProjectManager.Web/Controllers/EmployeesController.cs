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

            // 1. Create Identity account
            var user = new IdentityUser { UserName = model.Email, Email = model.Email, EmailConfirmed = true };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // 2. Assign role
                await _userManager.AddToRoleAsync(user, model.Role);

                // 3. Create employee record via service
                var employeeDto = new EmployeeDTO
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    UserId = user.Id // Link established immediately!
                };

                await _employeeService.AddEmployeeAsync(employeeDto);

                return RedirectToAction(nameof(Index));
            }

            // If Identity failed to create user (weak password, etc.)
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

        // --- Delete Methods ---
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            // 1. First find the employee to get their Email
            var employee = await _employeeService.GetEmployeeByIdAsync(id);
            if (employee == null) return NotFound();

            var userEmail = employee.Email; // Save email

            // 2. Remove from Employees table
            var success = await _employeeService.DeleteEmployeeAsync(id);

            if (success)
            {
                // 3. CRITICAL: Find user in AspNetUsers by Email
                // This works even if UserId link was broken
                var user = await _userManager.FindByEmailAsync(userEmail);

                if (user != null)
                {
                    var result = await _userManager.DeleteAsync(user);
                    if (!result.Succeeded)
                    {
                        // If deletion failed, show Identity errors
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        _logger.LogError($"Failed to delete Identity account: {errors}");
                        TempData["Error"] = $"Profile deleted, but system account remains: {errors}";
                    }
                }
                else
                {
                    _logger.LogWarning($"Account with email {userEmail} not found in AspNetUsers.");
                }

                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Deletion prohibited: employee is linked to tasks or projects.";
            return RedirectToAction(nameof(Index));
        }
    }
}
