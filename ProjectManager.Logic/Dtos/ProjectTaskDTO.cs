using System.ComponentModel.DataAnnotations;

namespace ProjectManager.Logic.Dtos
{
    public class ProjectTaskDTO
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Введите название задачи")]
        [StringLength(100, ErrorMessage = "Название слишком длинное")]
        public string Name { get; set; } = string.Empty;

        // Проект
        [Required(ErrorMessage = "Выберите проект")]
        public int ProjectId { get; set; }
        public string? ProjectName { get; set; }

        // Автор
        public int AuthorId { get; set; }
        public string? AuthorFullName { get; set; }

        // Исполнитель
        [Required(ErrorMessage = "Выберите исполнителя")]
        public int ExecutorId { get; set; }

        public string? ExecutorUserId { get; set; }

        
        public string? ExecutorFullName { get; set; }

        // Данные задачи
        public int Status { get; set; } // Можно передавать int для выпадающего списка
        public string? StatusDisplay { get; set; } // "ToDo", "InProgress" и т.д.

        public int Priority { get; set; }
        public string? Comment { get; set; }
    }
}