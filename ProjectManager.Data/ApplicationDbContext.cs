using Microsoft.EntityFrameworkCore;
using ProjectManager.Data.Entities;

namespace ProjectManager.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Project> Projects { get; set; }
        public DbSet<Employee> Employees { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Настройка связи "Многие-ко-многим" (Команда)
            modelBuilder.Entity<Project>()
                .HasMany(p => p.Employees)
                .WithMany(e => e.Projects);

            // 2. Настройка связи "Один-ко-многим" (Руководитель)
            // Мы явно связываем коллекцию SupervisedProjects с внешним ключом SupervisorId
            modelBuilder.Entity<Project>()
                .HasOne(p => p.Supervisor)
                .WithMany(e => e.SupervisedProjects) // <-- Указываем имя того самого свойства из ошибки
                .HasForeignKey(p => p.SupervisorId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}