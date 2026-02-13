using System.ComponentModel.DataAnnotations;

namespace ProjectManager.Logic.Dtos
{
    public class ProjectTaskDTO
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Enter task name")]
        [StringLength(100, ErrorMessage = "Name is too long")]
        public string Name { get; set; } = string.Empty;

        // Project
        [Required(ErrorMessage = "Select a project")]
        public int ProjectId { get; set; }
        public string? ProjectName { get; set; }

        // Author
        public int AuthorId { get; set; }
        public string? AuthorFullName { get; set; }

        // Executor
        [Required(ErrorMessage = "Select an executor")]
        public int ExecutorId { get; set; }

        public string? ExecutorUserId { get; set; }

        
        public string? ExecutorFullName { get; set; }

        // Task Data
        public int Status { get; set; } // Can pass int for dropdown list
        public string? StatusDisplay { get; set; } // "ToDo", "InProgress" etc.

        public int Priority { get; set; }
        public string? Comment { get; set; }
    }
}