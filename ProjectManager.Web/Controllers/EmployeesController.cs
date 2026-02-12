using Microsoft.AspNetCore.Mvc;
using ProjectManager.Logic.Dtos;
using ProjectManager.Logic.Services;

namespace ProjectManager.Web.Controllers
{
    public class EmployeesController : Controller
    {

        private readonly IEmployeeService _employeeService;

        public EmployeesController(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }
        public async Task<IActionResult> Index()
        {
            var employees = await _employeeService.GetAllEmployeeAsync();
            return View(employees);
        }

        [HttpGet]
        public IActionResult Create() { return View(); }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeDTO model)
        {
            if (ModelState.IsValid)
            {
                await _employeeService.AddEmployeeAsync(model);
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Search(string term)
        {
            var results = await _employeeService.SearchEmployeeAsync(term);
            return Json(results);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var employee = await _employeeService.GetEmployeeByIdAsync(id);
            if (employee == null) return NotFound();

            return View(employee);
        }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _employeeService.DeleteEmployeeAsync(id);
            if (!success)
            {
                // Например, если сотрудник — руководитель проекта, удалять нельзя
                TempData["Error"] = "Не удалось удалить сотрудника. Возможно, он назначен руководителем проекта.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
