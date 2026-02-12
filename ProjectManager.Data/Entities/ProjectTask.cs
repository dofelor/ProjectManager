using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectManager.Data.Entities
{

    public enum TaskStatus
    {
        ToDo,
        InProgress,
        Done
    }

    public class ProjectTask
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Comment { get; set; }
        public int Priority { get; set; }
        public TaskStatus Status { get; set; } = TaskStatus.ToDo;

        // Связь с Проектом
        public int ProjectId { get; set; }
        public Project? Project { get; set; }

        // Автор (Сотрудник)
        public int AuthorId { get; set; }
        public Employee? Author { get; set; }

        // Исполнитель (Сотрудник)
        public int ExecutorId { get; set; }
        public Employee? Executor { get; set; }
    }
}
