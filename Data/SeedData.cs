using Microsoft.AspNetCore.Identity;
using AcademicManagementSystemV4.Models;

namespace AcademicManagementSystemV4.Data;

public class SeedData
{
    public static async Task Initialize(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
    {
        // Create roles
        string[] roles = { "Student", "Staff", "Administrator" };
        foreach (string role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Create demo student user
        if (await userManager.FindByEmailAsync("student@wgu.edu") == null)
        {
            var studentUser = new ApplicationUser
            {
                UserName = "student@wgu.edu",
                Email = "student@wgu.edu",
                FirstName = "John",
                LastName = "Student",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(studentUser, "Password123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(studentUser, "Student");

                // Add sample data
                await SeedSampleData(context, studentUser);
            }
        }

        // Create demo staff user
        if (await userManager.FindByEmailAsync("advisor@wgu.edu") == null)
        {
            var staffUser = new ApplicationUser
            {
                UserName = "advisor@wgu.edu",
                Email = "advisor@wgu.edu",
                FirstName = "Jane",
                LastName = "Advisor",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(staffUser, "Password123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(staffUser, "Staff");
            }
        }
    }

    private static async Task SeedSampleData(ApplicationDbContext context, ApplicationUser user)
    {
        var term = new Term
        {
            Name = "Fall 2025",
            StartDate = new DateTime(2025, 9, 1),
            EndDate = new DateTime(2025, 12, 15),
            Description = "Fall semester 2025",
            UserId = user.Id
        };

        context.Terms.Add(term);
        await context.SaveChangesAsync();

        var course = new Course
        {
            CourseNumber = "CS101",
            Title = "Introduction to Computer Science",
            Description = "Fundamental concepts of computer science",
            CreditHours = 3,
            StartDate = new DateTime(2025, 9, 1),
            EndDate = new DateTime(2025, 12, 15),
            Status = CourseStatus.InProgress,
            TermId = term.Id
        };

        context.Courses.Add(course);
        await context.SaveChangesAsync();

        var assessment = new Assessment
        {
            Name = "Midterm Exam",
            Description = "Comprehensive midterm examination",
            Type = AssessmentType.Exam,
            DueDate = new DateTime(2025, 10, 15),
            Status = AssessmentStatus.NotStarted,
            MaxPoints = 100,
            CourseId = course.Id
        };

        context.Assessments.Add(assessment);
        await context.SaveChangesAsync();
    }
}

