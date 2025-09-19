using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AcademicManagementSystemV4.Models;

namespace AcademicManagementSystemV4.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Term> Terms { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<Assessment> Assessments { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure relationships
        builder.Entity<Term>()
            .HasOne(t => t.User)
            .WithMany(u => u.Terms)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Course>()
            .HasOne(c => c.Term)
            .WithMany(t => t.Courses)
            .HasForeignKey(c => c.TermId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Assessment>()
            .HasOne(a => a.Course)
            .WithMany(c => c.Assessments)
            .HasForeignKey(a => a.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes for performance
        builder.Entity<Term>()
            .HasIndex(t => t.UserId);

        builder.Entity<Course>()
            .HasIndex(c => c.TermId);

        builder.Entity<Assessment>()
            .HasIndex(a => a.CourseId);

        builder.Entity<Assessment>()
            .HasIndex(a => a.DueDate);
    }
}

