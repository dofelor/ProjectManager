using Microsoft.AspNetCore.Mvc;
using ProjectManager.Logic.Dtos;
using ProjectManager.Logic.Services;
using ProjectManager.Web.Models;

namespace ProjectManager.Web.Controllers
{
    public class ProjectsController : Controller
    {
        private readonly IProjectService _projectService;
        private readonly IEmployeeService _employeeService;

        public ProjectsController(IProjectService projectService, IEmployeeService employeeService)
        {
            _projectService = projectService;
            _employeeService = employeeService;
        }

        

        // 1. Список проектов с фильтрацией и сортировкой
        [HttpGet]
        public async Task<IActionResult> Index(int? priority, DateTime? dateFrom, DateTime? dateTo, string? sortBy)
        {
            // Вызываем метод сервиса, передавая все фильтры
            var projects = await _projectService.GetAllProjectAsync(priority, dateFrom, dateTo, sortBy);

            // Сохраняем текущую сортировку в ViewBag, чтобы ссылки в таблице могли её менять
            ViewBag.CurrentSort = sortBy;

            return View(projects);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var viewModel = new ProjectFormViewModel
            {
                Project = new ProjectDTO
                {
                    StartDate = DateTime.Today,
                    EndDate = DateTime.Today.AddMonths(1),
                    Priority = 1
                },
                AllEmployees = await _employeeService.GetAllEmployeeAsync()
            };
            return View(viewModel); // Передаем строго типизированную модель
        }

        // 2. Обработка данных из Визарда (сохранение проекта)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjectFormViewModel model, IFormFileCollection ProjectFiles)
        {
            if (ModelState.IsValid)
            {
                // 1. Создаем проект в базе через сервис
                // (передаем model.Project и SelectedEmployeeIds)

                // 2. Обработка файлов
                if (ProjectFiles != null && ProjectFiles.Count > 0)
                {
                    foreach (var file in ProjectFiles)
                    {
                        // Путь к папке (например, wwwroot/uploads)
                        var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

                        if (!Directory.Exists(uploadsPath))
                            Directory.CreateDirectory(uploadsPath);

                        var filePath = Path.Combine(uploadsPath, file.FileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream); // Сохраняем файл на диск
                        }

                        // Тут можно сохранить путь к файлу в отдельную таблицу базы данных ProjectFiles
                    }
                }

                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // 3. Редактирование (Загрузка данных)
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var project = await _projectService.GetProjectByIdAsync(id);
            if (project == null) return NotFound();

            ViewBag.Employees = await _employeeService.GetAllEmployeeAsync();
            return View(project);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProjectDTO model, List<int> employeeIds)
        {
            if (model.EndDate < model.StartDate)
            {
                ModelState.AddModelError("", "Дата окончания не может быть раньше даты начала!");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _projectService.UpdateProjectAsync(id, model, employeeIds);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
                
            }
            ViewBag.Employees = await _employeeService.GetAllEmployeeAsync();
            return View(model);
        }



        // 4. Удаление
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _projectService.DeleteProjectAsync(id);
            return RedirectToAction(nameof(Index));
        }

        
    }
}
