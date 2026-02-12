using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProjectManager.Logic.Dtos;
using ProjectManager.Logic.Services;
using ProjectManager.Web.Models;

namespace ProjectManager.Web.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly IProjectService _projectService;
        private readonly IEmployeeService _employeeService;
        private readonly IProjectTaskService _taskService;
        private readonly UserManager<IdentityUser> _userManager; // Добавили

        public ProjectsController(
            IProjectService projectService,
            IEmployeeService employeeService,
            IProjectTaskService taskService,
            UserManager<IdentityUser> userManager) // Внедряем
        {
            _projectService = projectService;
            _employeeService = employeeService;
            _taskService = taskService;
            _userManager = userManager;
        }

        // 1. Список проектов с фильтрацией по ролям
        [HttpGet]
        public async Task<IActionResult> Index(int? priority, DateTime? dateFrom, DateTime? dateTo, string? sortBy)
        {
            var allProjects = await _projectService.GetAllProjectAsync(priority, dateFrom, dateTo, sortBy);
            var currentUserId = _userManager.GetUserId(User);

            if (User.IsInRole("ProjectManager"))
            {
                // 1. Находим Id сотрудника для текущего менеджера
                var employees = await _employeeService.GetAllEmployeeAsync();
                var currentEmployee = employees.FirstOrDefault(e => e.UserId == currentUserId);

                if (currentEmployee != null)
                {
                    // 2. Оставляем проекты, где он Supervisor ИЛИ член команды
                    allProjects = allProjects.Where(p =>
                        p.SupervisorId == currentEmployee.Id ||
                        (p.Employees != null && p.Employees.Any(e => e.UserId == currentUserId))
                    ).ToList();
                }
            }
            else if (User.IsInRole("Employee"))
            {
                allProjects = allProjects.Where(p =>
                    p.Employees != null && p.Employees.Any(e => e.UserId == currentUserId)
                ).ToList();
            }

            ViewBag.CurrentSort = sortBy;
            return View(allProjects);
        }

        [Authorize(Roles = "Admin")]
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
            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjectFormViewModel model, IFormFileCollection ProjectFiles)
        {
            // 1. Проверяем валидность модели
            if (ModelState.IsValid)
            {
                try
                {
                    // 2. ВЫЗОВ СЕРВИСА (был закомментирован)
                    // Передаем данные проекта и список ID сотрудников, выбранных в визарде
                    await _projectService.CreateProjectAsync(model.Project, model.SelectedEmployeeIds);

                    // 3. Обработка файлов
                    if (ProjectFiles != null && ProjectFiles.Count > 0)
                    {
                        var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                        if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);

                        foreach (var file in ProjectFiles)
                        {
                            var filePath = Path.Combine(uploadsPath, file.FileName);
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }
                        }
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Ошибка при создании проекта: " + ex.Message);
                }
            }

            // Если форма невалидна, нужно заново наполнить список сотрудников для Dropdown
            model.AllEmployees = await _employeeService.GetAllEmployeeAsync();
            return View(model);
        }

        [Authorize(Roles = "Admin,ProjectManager")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id, string? returnUrl)
        {
            var project = await _projectService.GetProjectByIdAsync(id);
            if (project == null) return NotFound();

            ViewBag.Employees = await _employeeService.GetAllEmployeeAsync();
            ViewBag.ReturnUrl = returnUrl ?? Request.Headers["Referer"].ToString();
            return View(project);
        }

        [Authorize(Roles = "Admin,ProjectManager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProjectDTO model, List<int> employeeIds, string? returnUrl)
        {
            // 1. Получаем существующий проект из БД для проверки прав
            var existingProject = await _projectService.GetProjectByIdAsync(id);
            if (existingProject == null) return NotFound();

            // 2. Проверка прав: Менеджер может редактировать только СВОИ проекты
            if (User.IsInRole("ProjectManager"))
            {
                var currentUserId = _userManager.GetUserId(User);
                var employees = await _employeeService.GetAllEmployeeAsync();
                var currentEmployee = employees.FirstOrDefault(e => e.UserId == currentUserId);

                // Если менеджер не является руководителем этого проекта — доступ запрещен
                if (currentEmployee == null || existingProject.SupervisorId != currentEmployee.Id)
                {
                    return Forbid();
                }
            }

            // 3. Валидация дат
            if (model.EndDate < model.StartDate)
            {
                ModelState.AddModelError("", "Дата окончания не может быть раньше даты начала!");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // 4. Обновление проекта и списка сотрудников (назначение/удаление)
                    await _projectService.UpdateProjectAsync(id, model, employeeIds);

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Ошибка при сохранении: " + ex.Message);
                }
            }

            // Если что-то пошло не так, возвращаем форму с данными
            ViewBag.Employees = await _employeeService.GetAllEmployeeAsync();
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        // Удаление доступно только Админам и Менеджерам
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var project = await _projectService.GetProjectByIdAsync(id);
            if (project == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            var employees = await _employeeService.GetAllEmployeeAsync();
            var currentEmployee = employees.FirstOrDefault(e => e.UserId == currentUserId);

            // ЗАЩИТА: Менеджер может удалять только свои проекты
            if (!User.IsInRole("Admin") && project.SupervisorId != currentEmployee?.Id)
            {
                return Forbid();
            }

            await _projectService.DeleteProjectAsync(id);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var project = await _projectService.GetProjectByIdAsync(id);
            if (project == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);

            // Получаем задачи для проекта
            var tasks = await _taskService.GetAllTasksAsync(projectId: id);

            // ФИЛЬТРАЦИЯ ЗАДАЧ ДЛЯ СОТРУДНИКА
            if (User.IsInRole("Employee"))
            {
                // В задачах поле исполнителя обычно называется ExecutorUserId (строка)
                tasks = tasks.Where(t => t.ExecutorUserId == currentUserId).ToList();
            }

            ViewBag.ProjectTasks = tasks;
            return View(project);
        }
    }
}