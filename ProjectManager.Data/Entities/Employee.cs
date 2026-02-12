using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // ДЛЯ [ForeignKey]
using Microsoft.AspNetCore.Identity; // ДЛЯ IdentityUser

namespace ProjectManager.Data.Entities
{
    public class Employee
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }

        [MaxLength(100)]
        public string? MiddleName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public List<Project> Projects { get; set; } = new();
        public List<Project> SupervisedProjects { get; set; } = new();

        // Связь с Identity
        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual IdentityUser? User { get; set; }
    }
}