using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ProjectManager.Logic.Dtos;
using ProjectManager.Logic.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace ProjectManager.Web.Controllers
{
    [Authorize]
    public class TasksController : Controller
    {
        private readonly IProjectTaskService _taskService;
        private readonly IProjectService _projectService;
        private readonly IEmployeeService _employeeService;
        private readonly UserManager<IdentityUser> _userManager;

        public TasksController(
            IProjectTaskService taskService,
            IProjectService projectService,
            IEmployeeService employeeService,
            UserManager<IdentityUser> userManager)
        {
            _taskService = taskService;
            _projectService = projectService;
            _employeeService = employeeService;
            _userManager = userManager;
        }

        // 1. Список задач с фильтрацией
        public async Task<IActionResult> Index(int? projectId, int? status)
        {
            var tasks = await _taskService.GetAllTasksAsync(projectId, status);
            var currentUserId = _userManager.GetUserId(User);

            if (User.IsInRole("Employee"))
            {
                // Обычный сотрудник видит только свои задачи
                tasks = tasks.Where(t => t.ExecutorUserId == currentUserId).ToList();
            }
            else if (User.IsInRole("ProjectManager"))
            {
                // Менеджер видит задачи во всех проектах, но управлять сможет только своими (проверка в Edit)
                // Опционально: можно отфильтровать список задач только для его проектов здесь
            }

            ViewBag.Projects = new SelectList(await _projectService.GetAllProjectAsync(), "Id", "Name");
            return View(tasks);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Create(int? projectId)
        {
            if (!projectId.HasValue) return BadRequest();

            // Добавляем проверку прав уже на входе!
            if (User.IsInRole("ProjectManager"))
            {
                if (!await IsProjectSupervisor(projectId.Value)) return Forbid();
            }

            await PopulateSelectListsAsync(projectId);
            var model = new ProjectTaskDTO { ProjectId = projectId.Value };
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,ProjectManager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjectTaskDTO model)
        {
            // 1. Проверка прав (оставляем как было)
            if (User.IsInRole("ProjectManager"))
            {
                if (!await IsProjectSupervisor(model.ProjectId)) return Forbid();
            }

            // 2. Ищем конкретно ЭТОГО сотрудника напрямую
            var currentUserId = _userManager.GetUserId(User);

            // Получаем список один раз и ищем по UserId (строка в строку)
            var allEmployees = await _employeeService.GetAllEmployeeAsync();
            var currentEmployee = allEmployees.FirstOrDefault(e =>
                !string.IsNullOrWhiteSpace(e.UserId) &&
                e.UserId.Trim().Equals(currentUserId.Trim(), StringComparison.OrdinalIgnoreCase));

            if (currentEmployee == null)
            {
                // Если зашли сюда — значит в таблице Employees нет записи с UserId = currentUserId
                ModelState.AddModelError("", $"Ошибка: Профиль сотрудника не найден для вашего UserId ({currentUserId}). Проверьте таблицу Employees.");
            }
            else
            {
                model.AuthorId = currentEmployee.Id;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _taskService.CreateTaskAsync(model);
                    return RedirectToAction("Details", "Projects", new { id = model.ProjectId });
                }
                catch (Exception ex)
                {
                    // Это поймает ту самую SqlException и выведет её на экран вместо белого окна
                    ModelState.AddModelError("", "Ошибка сохранения в БД. Убедитесь, что AuthorId корректен. " + ex.Message);
                }
            }

            await PopulateSelectListsAsync(model.ProjectId);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, string? returnUrl = null)
        {
            var taskDto = await _taskService.GetTaskByIdAsync(id);
            if (taskDto == null) return NotFound();

            // ПРОВЕРКА ПРАВ
            if (User.IsInRole("Employee"))
            {
                if (taskDto.ExecutorUserId != _userManager.GetUserId(User)) return Forbid();
            }
            else if (User.IsInRole("ProjectManager"))
            {
                if (!await IsProjectSupervisor(taskDto.ProjectId)) return Forbid();
            }

            await PopulateSelectListsAsync(taskDto.ProjectId);
            ViewBag.ReturnUrl = returnUrl ?? Request.Headers["Referer"].ToString();
            return View(taskDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProjectTaskDTO taskDto, string? returnUrl)
        {
            var existingTask = await _taskService.GetTaskByIdAsync(taskDto.Id);
            if (existingTask == null) return NotFound();

            // ПРОВЕРКА ПРАВ ПЕРЕД СОХРАНЕНИЕМ
            if (User.IsInRole("Employee"))
            {
                // Сотрудник может только менять статус, но мы проверяем владение
                if (existingTask.ExecutorUserId != _userManager.GetUserId(User)) return Forbid();

                // Чтобы сотрудник не подменил проект или автора через инспект кода:
                taskDto.ProjectId = existingTask.ProjectId;
                taskDto.AuthorId = existingTask.AuthorId;
                taskDto.Name = existingTask.Name; // Запрещаем менять название
            }
            else if (User.IsInRole("ProjectManager"))
            {
                if (!await IsProjectSupervisor(existingTask.ProjectId)) return Forbid();
            }

            if (ModelState.IsValid)
            {
                await _taskService.UpdateTaskAsync(taskDto);
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
                return RedirectToAction("Details", "Projects", new { id = taskDto.ProjectId });
            }

            await PopulateSelectListsAsync(taskDto.ProjectId);
            return View(taskDto);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,ProjectManager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string? returnUrl)
        {
            var task = await _taskService.GetTaskByIdAsync(id);
            if (task == null) return NotFound();

            if (User.IsInRole("ProjectManager"))
            {
                if (!await IsProjectSupervisor(task.ProjectId)) return Forbid();
            }

            await _taskService.DeleteTaskAsync(id);
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction(nameof(Index));
        }

        // Вспомогательный метод для проверки: является ли текущий юзер менеджером этого проекта
        private async Task<bool> IsProjectSupervisor(int projectId)
        {
            if (User.IsInRole("Admin")) return true;

            // 1. Получаем проект (в нем уже есть SupervisorUserId благодаря нашему сервису)
            var project = await _projectService.GetProjectByIdAsync(projectId);
            if (project == null) return false;

            // 2. Получаем ID текущего пользователя из Identity
            var currentUserId = _userManager.GetUserId(User);

            // 3. Сравниваем напрямую GUID-строки из Identity
            // Это самый надежный способ, так как он не зависит от таблицы Employees
            return project.SupervisorUserId == currentUserId;
        }

        private async Task PopulateSelectListsAsync(int? projectId = null)
        {
            var projects = await _projectService.GetAllProjectAsync();
            var employees = await _employeeService.GetAllEmployeeAsync();

            // Если это PM, он может назначать только тех сотрудников, которые есть в системе
            // (Или можно отфильтровать только участников проекта)

            ViewBag.ProjectId = new SelectList(projects, "Id", "Name", projectId);

            var employeeItems = employees.Select(e => new {
                Id = e.Id,
                FullName = $"{e.LastName} {e.FirstName}"
            }).OrderBy(e => e.FullName);

            ViewBag.AuthorId = new SelectList(employeeItems, "Id", "FullName");
            ViewBag.ExecutorId = new SelectList(employeeItems, "Id", "FullName");

            // Статусы задач (пример)
            var statuses = new List<SelectListItem>
            {
                new SelectListItem { Value = "0", Text = "Новая" },
                new SelectListItem { Value = "1", Text = "В работе" },
                new SelectListItem { Value = "2", Text = "Завершена" }
            };
            ViewBag.Status = new SelectList(statuses, "Value", "Text");
        }

        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager,Employee")]
        public async Task<IActionResult> Details(int id)
        {
            var task = await _taskService.GetTaskByIdAsync(id);

            if (task == null)
            {
                return NotFound();
            }

            // Проверка прав доступа: 
            // Если это обычный сотрудник, он должен видеть только те задачи, где он исполнитель
            if (User.IsInRole("Employee"))
            {
                var currentUserId = _userManager.GetUserId(User);
                if (task.ExecutorUserId != currentUserId)
                {
                    return Forbid();
                }
            }
            // Менеджеры и админы могут смотреть любые задачи (или добавь IsProjectSupervisor, если нужно ограничить PM-ов)

            return View(task);
        }
    }
}