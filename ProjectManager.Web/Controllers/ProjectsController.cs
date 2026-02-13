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
        private readonly UserManager<IdentityUser> _userManager;

        public ProjectsController(
            IProjectService projectService,
            IEmployeeService employeeService,
            IProjectTaskService taskService,
            UserManager<IdentityUser> userManager)
        {
            _projectService = projectService;
            _employeeService = employeeService;
            _taskService = taskService;
            _userManager = userManager;
        }

        // 1. List of projects with role filtering
        [HttpGet]
        public async Task<IActionResult> Index(int? priority, DateTime? dateFrom, DateTime? dateTo, string? sortBy)
        {
            var allProjects = await _projectService.GetAllProjectAsync(priority, dateFrom, dateTo, sortBy);
            var currentUserId = _userManager.GetUserId(User);

            if (User.IsInRole("ProjectManager"))
            {
                // 1. Find Employee Id for current manager
                var employees = await _employeeService.GetAllEmployeeAsync();
                var currentEmployee = employees.FirstOrDefault(e => e.UserId == currentUserId);

                if (currentEmployee != null)
                {
                    // 2. Keep projects where they are Supervisor OR team member
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
            // 1. Check model validity
            if (ModelState.IsValid)
            {
                try
                {
                    // 2. SERVICE CALL
                    // Pass project data and selected employee IDs
                    await _projectService.CreateProjectAsync(model.Project, model.SelectedEmployeeIds);

                    // 3. File processing
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
                    ModelState.AddModelError("", "Error creating project: " + ex.Message);
                }
            }

            // If form is invalid, repopulate employee list for Dropdown
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
            // 1. Get existing project from DB to check permissions
            var existingProject = await _projectService.GetProjectByIdAsync(id);
            if (existingProject == null) return NotFound();

            // 2. Check permissions: Manager can only edit THEIR OWN projects
            if (User.IsInRole("ProjectManager"))
            {
                var currentUserId = _userManager.GetUserId(User);
                var employees = await _employeeService.GetAllEmployeeAsync();
                var currentEmployee = employees.FirstOrDefault(e => e.UserId == currentUserId);

                // If manager is not the supervisor of this project — access denied
                if (currentEmployee == null || existingProject.SupervisorId != currentEmployee.Id)
                {
                    return Forbid();
                }
            }

            // 3. Date validation
            if (model.EndDate < model.StartDate)
            {
                ModelState.AddModelError("", "End date cannot be earlier than start date!");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // 4. Update project and employee list (assign/remove)
                    await _projectService.UpdateProjectAsync(id, model, employeeIds);

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error saving: " + ex.Message);
                }
            }

            // If something went wrong, return form with data
            ViewBag.Employees = await _employeeService.GetAllEmployeeAsync();
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        // Details available only to Admins and Managers
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

            // PROTECTION: Manager can only delete their own projects
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

            // Get tasks for project
            var tasks = await _taskService.GetAllTasksAsync(projectId: id);

            // TASK FILTERING FOR EMPLOYEE
            if (User.IsInRole("Employee"))
            {
                // In tasks, executor field is usually ExecutorUserId (string)
                tasks = tasks.Where(t => t.ExecutorUserId == currentUserId).ToList();
            }

            ViewBag.ProjectTasks = tasks;
            return View(project);
        }
    }
}