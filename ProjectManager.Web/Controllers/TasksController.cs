using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ProjectManager.Logic.Dtos;
using ProjectManager.Logic.Services;

namespace ProjectManager.Web.Controllers
{
    public class TasksController : Controller
    {
        private readonly IProjectTaskService _taskService;
        private readonly IProjectService _projectService; // Нужен для списка проектов
        private readonly IEmployeeService _employeeService;
        public TasksController(IProjectTaskService taskService, IProjectService projectService, IEmployeeService employeeService)
        {
            _taskService = taskService;
            _projectService = projectService;
            _employeeService = employeeService;
        }

        public async Task<IActionResult> Index(int? projectId, int? status)
        {
            var tasks = await _taskService.GetAllTasksAsync(projectId, status);

            // Передаем список проектов для фильтра в представлении
            ViewBag.Projects = new SelectList(await _projectService.GetAllProjectAsync(), "Id", "Name");
            return View(tasks);
        }


        [HttpGet]
        public async Task<IActionResult> Create(int? projectId) // Принимаем ID из URL
        {
            await PopulateSelectListsAsync();

            // Создаем пустую DTO и, если projectId передан, записываем его
            var model = new ProjectTaskDTO();
            if (projectId.HasValue)
            {
                model.ProjectId = projectId.Value;
            }

            return View(model);
        }

        // Создание задачи (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjectTaskDTO taskDto)
        {
            if (ModelState.IsValid)
            {
                await _taskService.CreateTaskAsync(taskDto);
                return RedirectToAction(nameof(Index));
            }

            await PopulateSelectListsAsync();
            return View(taskDto);
        }

        // Вспомогательный метод для заполнения списков
        private async Task PopulateSelectListsAsync()
        {
            var projects = await _projectService.GetAllProjectAsync();
            var employees = await _employeeService.GetAllEmployeeAsync();

            ViewBag.ProjectId = new SelectList(projects, "Id", "Name");

            // Создаем список сотрудников с полным именем для выбора
            var employeeItems = employees.Select(e => new {
                Id = e.Id,
                FullName = $"{e.FirstName} {e.LastName}"
            });

            ViewBag.AuthorId = new SelectList(employeeItems, "Id", "FullName");
            ViewBag.ExecutorId = new SelectList(employeeItems, "Id", "FullName");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, string? returnUrl = null)
        {
            var taskDto = await _taskService.GetTaskByIdAsync(id);
            if (taskDto == null) return NotFound();

            await PopulateSelectListsAsync();

            // Если returnUrl не передан, берем его из заголовка Referer (откуда пришли)
            ViewBag.ReturnUrl = returnUrl ?? Request.Headers["Referer"].ToString();

            return View(taskDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProjectTaskDTO taskDto, string? returnUrl)
        {
            if (ModelState.IsValid)
            {
                await _taskService.UpdateTaskAsync(taskDto);

                // Если у нас есть обратный адрес и он "местный", идем туда
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction(nameof(Index));
            }

            await PopulateSelectListsAsync();
            return View(taskDto);
        }

        // Удаление задачи (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string? returnUrl)
        {
            // Если нам передали returnUrl (например, адрес страницы деталей проекта), 
            // мы просто удаляем и возвращаемся туда
            await _taskService.DeleteTaskAsync(id);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id, string? returnUrl = null)
        {
            var task = await _taskService.GetTaskByIdAsync(id);
            if (task == null) return NotFound();

            ViewBag.ReturnUrl = returnUrl ?? Request.Headers["Referer"].ToString();
            return View(task);
        }
    }
}

