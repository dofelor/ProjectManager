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

        // 1. Task list with filtering
        public async Task<IActionResult> Index(int? projectId, int? status)
        {
            var tasks = await _taskService.GetAllTasksAsync(projectId, status);
            var currentUserId = _userManager.GetUserId(User);

            if (User.IsInRole("Employee"))
            {
                // Regular employee sees only their own tasks
                tasks = tasks.Where(t => t.ExecutorUserId == currentUserId).ToList();
            }
            else if (User.IsInRole("ProjectManager"))
            {
                // Manager sees tasks in all projects, but can manage only their own (check in Edit)
                // Optional: filter task list for their projects here
            }

            ViewBag.Projects = new SelectList(await _projectService.GetAllProjectAsync(), "Id", "Name");
            return View(tasks);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Create(int? projectId)
        {
            if (!projectId.HasValue) return BadRequest();

            // Add permission check at entry!
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
            // 1. Permission check (keep as is)
            if (User.IsInRole("ProjectManager"))
            {
                if (!await IsProjectSupervisor(model.ProjectId)) return Forbid();
            }

            // 2. Find THIS specific employee directly
            var currentUserId = _userManager.GetUserId(User);

            // Get list once and search by UserId (exact match)
            var allEmployees = await _employeeService.GetAllEmployeeAsync();
            var currentEmployee = allEmployees.FirstOrDefault(e =>
                !string.IsNullOrWhiteSpace(e.UserId) &&
                e.UserId.Trim().Equals(currentUserId.Trim(), StringComparison.OrdinalIgnoreCase));

            if (currentEmployee == null)
            {
                // If here — no record in Employees table with UserId = currentUserId
                ModelState.AddModelError("", $"Error: Employee profile not found for your UserId ({currentUserId}). Check Employees table.");
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
                    // Catches SqlException and shows it on screen instead of white page
                    ModelState.AddModelError("", "Error saving to DB. Ensure AuthorId is correct. " + ex.Message);
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

            // PERMISSION CHECK
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

            // PERMISSION CHECK BEFORE SAVE
            if (User.IsInRole("Employee"))
            {
                // Employee can only change status, but we check ownership
                if (existingTask.ExecutorUserId != _userManager.GetUserId(User)) return Forbid();

                // Prevent employee from swapping project or author via code inspection:
                taskDto.ProjectId = existingTask.ProjectId;
                taskDto.AuthorId = existingTask.AuthorId;
                taskDto.Name = existingTask.Name; // Prevent name change
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

        // Helper method to check if current user is manager of this project
        private async Task<bool> IsProjectSupervisor(int projectId)
        {
            if (User.IsInRole("Admin")) return true;

            // 1. Get project (SupervisorUserId is already there thanks to service)
            var project = await _projectService.GetProjectByIdAsync(projectId);
            if (project == null) return false;

            // 2. Get current user ID from Identity
            var currentUserId = _userManager.GetUserId(User);

            // 3. Compare Identity GUID strings directly
            // Most reliable method as it doesn't depend on Employees table
            return project.SupervisorUserId == currentUserId;
        }

        private async Task PopulateSelectListsAsync(int? projectId = null)
        {
            var projects = await _projectService.GetAllProjectAsync();
            var employees = await _employeeService.GetAllEmployeeAsync();

            // If PM, can only assign employees existing in system
            // (Or filter only project participants)

            ViewBag.ProjectId = new SelectList(projects, "Id", "Name", projectId);

            var employeeItems = employees.Select(e => new {
                Id = e.Id,
                FullName = $"{e.LastName} {e.FirstName}"
            }).OrderBy(e => e.FullName);

            ViewBag.AuthorId = new SelectList(employeeItems, "Id", "FullName");
            ViewBag.ExecutorId = new SelectList(employeeItems, "Id", "FullName");

            // Task statuses (example)
            var statuses = new List<SelectListItem>
            {
                new SelectListItem { Value = "0", Text = "New" },
                new SelectListItem { Value = "1", Text = "In Progress" },
                new SelectListItem { Value = "2", Text = "Completed" }
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

            // Access control check: 
            // If regular employee, should see only tasks where they are executor
            if (User.IsInRole("Employee"))
            {
                var currentUserId = _userManager.GetUserId(User);
                if (task.ExecutorUserId != currentUserId)
                {
                    return Forbid();
                }
            }
            // Managers and admins can view any tasks (or add IsProjectSupervisor to restrict PMs)

            return View(task);
        }
    }
}