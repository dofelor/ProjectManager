using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using ProjectManager.Data;
using ProjectManager.Data.Entities;
using ProjectManager.Logic.Dtos;
using ProjectManager.Logic.Services;
using MyTaskStatus = ProjectManager.Data.Entities.TaskStatus;

namespace ProjectManager.Tests
{
    [TestFixture] // Атрибут NUnit для тестового класса
    public class ProjectTaskServiceTests
    {
        private ApplicationDbContext _context;
        private ProjectTaskService _service;

        [SetUp] // Выполняется перед каждым тестом
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();
            _service = new ProjectTaskService(_context);
        }

        [TearDown] // Выполняется после каждого теста
        public void TearDown()
        {
            _context.Dispose();
        }

        [Test] // Атрибут NUnit для самого теста
        public async Task CreateTaskAsync_ShouldSaveTaskCorrectly()
        {
            // Arrange
            var dto = new ProjectTaskDTO
            {
                Name = "Unit Test Task",
                ProjectId = 1,
                AuthorId = 1,
                ExecutorId = 2,
                Status = 0
            };

            // Act
            await _service.CreateTaskAsync(dto);

            // Assert
            var taskInDb = await _context.ProjectTasks.FirstOrDefaultAsync(t => t.Name == "Unit Test Task");
            Assert.That(taskInDb, Is.Not.Null);
            Assert.That(taskInDb.Status, Is.EqualTo(MyTaskStatus.ToDo));
        }

        [Test]
        public async Task GetAllTasksAsync_ShouldReturnOnlyFilteredTasks()
        {
            // 1. Arrange - Создаем "фундамент" (Сотрудника и Проект)
            var employee = new Employee
            {
                Id = 1,
                FirstName = "Ivan",
                LastName = "Ivanov",
                Email = "ivan@test.com" // Обязательное поле из прошлой ошибки
            };

            var project = new Project
            {
                Id = 1,
                Name = "Alpha",
                CustomerCompany = "Test Customer",
                PerformingCompany = "Test Executor",
                // ДОБАВЛЯЕМ ОБЯЗАТЕЛЬНЫЕ ДАТЫ:
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(1)
            };

            _context.Employees.Add(employee);
            _context.Projects.Add(project);

            // 2. Добавляем сами задачи
            _context.ProjectTasks.AddRange(
                new ProjectTask
                {
                    Id = 1,
                    Name = "Active Task",
                    Status = MyTaskStatus.InProgress,
                    ProjectId = 1,
                    AuthorId = 1,
                    ExecutorId = 1
                },
                new ProjectTask
                {
                    Id = 2,
                    Name = "Done Task",
                    Status = MyTaskStatus.Done,
                    ProjectId = 1,
                    AuthorId = 1,
                    ExecutorId = 1
                }
            );

            await _context.SaveChangesAsync();

            // 3. Act - запрашиваем задачи со статусом Done (2)
            var result = await _service.GetAllTasksAsync(status: 2);

            // 4. Assert - проверка
            Assert.That(result.Count(), Is.EqualTo(1));
            Assert.That(result.First().Name, Is.EqualTo("Done Task"));
        }
    }
}