using Microsoft.EntityFrameworkCore;
using ProjectManager.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace ProjectManager.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Project> Projects { get; set; }
        public DbSet<Employee> Employees { get; set; }

        public DbSet<ProjectTask> ProjectTasks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Configure "Many-to-Many" relationship (Team)
            modelBuilder.Entity<Project>()
                .HasMany(p => p.Employees)
                .WithMany(e => e.Projects);

            // 2. Configure "One-to-Many" relationship (Supervisor)
            // Explicitly bind the SupervisedProjects collection to the SupervisorId foreign key
            modelBuilder.Entity<Project>()
                .HasOne(p => p.Supervisor)
                .WithMany(e => e.SupervisedProjects)
                .HasForeignKey(p => p.SupervisorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProjectTask>()
                .HasOne(t => t.Author)
                .WithMany()
                .HasForeignKey(t => t.AuthorId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent deletion of employee if they have associated tasks

            modelBuilder.Entity<ProjectTask>()
                .HasOne(t => t.Executor)
                .WithMany()
                .HasForeignKey(t => t.ExecutorId)
                .OnDelete(DeleteBehavior.Restrict);
        }


    }
}