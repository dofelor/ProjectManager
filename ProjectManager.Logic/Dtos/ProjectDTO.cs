namespace ProjectManager.Logic.Dtos
{
    public class ProjectDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string CustomerCompany { get; set; }
        public string PerformingCompany { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Priority { get; set; }

        public int SupervisorId { get; set; }
        public string? SupervisorFullName { get; set; }

        // Stores UserId (GUID) from AspNetUsers table
        public string? SupervisorUserId { get; set; }

        public List<EmployeeDTO> Employees { get; set; } = new();
    }
}