namespace ProjectManager.Logic.Dtos
{
    public class EmployeeDTO
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? MiddleName { get; set; }
        public string Email { get; set; }

        // ОБЯЗАТЕЛЬНО ДОБАВЬ ЭТО:
        public string? UserId { get; set; }

        public string FullName => $"{LastName} {FirstName} {MiddleName}".Trim();
    }
}