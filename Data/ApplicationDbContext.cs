// Data/ApplicationDbContext.cs
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AcademicManagementSystemV4.Models;

namespace AcademicManagementSystemV4.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Term> Terms { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Assessment> Assessments { get; set; }
        public DbSet<CourseTemplate> CourseTemplates { get; set; }
        public DbSet<AssessmentTemplate> AssessmentTemplates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Terms
            modelBuilder.Entity<Term>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100); // SQLite-friendly
                entity.Property(e => e.Description)
                    .HasMaxLength(500); // SQLite-friendly
                entity.Property(e => e.UserId)
                    .IsRequired()
                    .HasMaxLength(450); // Standard Identity UserId length
                entity.Property(e => e.StartDate)
                    .IsRequired()
                    .HasColumnType("TEXT"); // SQLite date format
                entity.Property(e => e.EndDate)
                    .IsRequired()
                    .HasColumnType("TEXT"); // SQLite date format
                entity.Property(e => e.CreatedAt)
                    .HasColumnType("TEXT")
                    .HasDefaultValueSql("datetime('now')");
                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("TEXT")
                    .HasDefaultValueSql("datetime('now')");

                // Index
                entity.HasIndex(e => e.UserId);
            });

            // Configure Courses
            modelBuilder.Entity<Course>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CourseNumber)
                    .IsRequired()
                    .HasMaxLength(20);
                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(200);
                entity.Property(e => e.Description)
                    .HasMaxLength(1000);
                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(50);
                entity.Property(e => e.StartDate)
                    .IsRequired()
                    .HasColumnType("TEXT");
                entity.Property(e => e.EndDate)
                    .IsRequired()
                    .HasColumnType("TEXT");
                entity.Property(e => e.CreatedAt)
                    .HasColumnType("TEXT")
                    .HasDefaultValueSql("datetime('now')");
                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("TEXT")
                    .HasDefaultValueSql("datetime('now')");

                // Relationship
                entity.HasOne(c => c.Term)
                    .WithMany(t => t.Courses)
                    .HasForeignKey(c => c.TermId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Index
                entity.HasIndex(e => e.TermId);
            });

            // Configure Assessments
            modelBuilder.Entity<Assessment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(200);
                entity.Property(e => e.Description)
                    .HasMaxLength(1000);
                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(50);
                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(50);
                entity.Property(e => e.DueDate)
                    .IsRequired()
                    .HasColumnType("TEXT");
                entity.Property(e => e.Score)
                    .HasColumnType("REAL");
                entity.Property(e => e.MaxPoints)
                    .HasColumnType("REAL")
                    .HasDefaultValue(100.0);
                entity.Property(e => e.CreatedAt)
                    .HasColumnType("TEXT")
                    .HasDefaultValueSql("datetime('now')");
                entity.Property(e => e.UpdatedAt)
                    .HasColumnType("TEXT")
                    .HasDefaultValueSql("datetime('now')");

                // Relationship
                entity.HasOne(a => a.Course)
                    .WithMany(c => c.Assessments)
                    .HasForeignKey(a => a.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Indexes
                entity.HasIndex(e => e.CourseId);
                entity.HasIndex(e => e.DueDate);
            });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Fallback configuration for SQLite
                optionsBuilder.UseSqlite("Data Source=app.db");
            }
        }
    }
}