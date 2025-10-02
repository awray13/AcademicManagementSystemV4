using Microsoft.AspNetCore.Identity;
using AcademicManagementSystemV4.Models;
using Microsoft.EntityFrameworkCore;

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
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(studentUser, "Password123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(studentUser, "Student");

                // Add comprehensive sample data
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
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(staffUser, "Password123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(staffUser, "Staff");
            }
        }

        await context.SaveChangesAsync();
        await SeedCourseTemplates(context);
    }

    private static async Task SeedSampleData(ApplicationDbContext context, ApplicationUser user)
    {
        try
        {
            // Create multiple terms
            var terms = new List<Term>
            {
                new Term
                {
                    Name = "Fall 2024",
                    StartDate = new DateTime(2024, 9, 1),
                    EndDate = new DateTime(2024, 12, 15),
                    Description = "Fall semester 2024",
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Term
                {
                    Name = "Spring 2025",
                    StartDate = new DateTime(2025, 1, 15),
                    EndDate = new DateTime(2025, 5, 10),
                    Description = "Spring semester 2025",
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Term
                {
                    Name = "Summer 2025",
                    StartDate = new DateTime(2025, 6, 1),
                    EndDate = new DateTime(2025, 8, 15),
                    Description = "Summer semester 2025",
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            context.Terms.AddRange(terms);
            await context.SaveChangesAsync();

            // Create courses for each term
            var courses = new List<Course>
            {
                // Fall 2024 courses
                new Course
                {
                    CourseNumber = "CS101",
                    Title = "Introduction to Computer Science",
                    Description = "Fundamental concepts of computer science and programming",
                    CreditHours = 3,
                    StartDate = new DateTime(2024, 9, 1),
                    EndDate = new DateTime(2024, 12, 15),
                    Status = CourseStatus.Completed,
                    TermId = terms[0].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Course
                {
                    CourseNumber = "MATH201",
                    Title = "Calculus I",
                    Description = "Introduction to differential and integral calculus",
                    CreditHours = 4,
                    StartDate = new DateTime(2024, 9, 1),
                    EndDate = new DateTime(2024, 12, 15),
                    Status = CourseStatus.Completed,
                    TermId = terms[0].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                // Spring 2025 courses
                new Course
                {
                    CourseNumber = "CS201",
                    Title = "Data Structures and Algorithms",
                    Description = "Advanced programming concepts and algorithm design",
                    CreditHours = 3,
                    StartDate = new DateTime(2025, 1, 15),
                    EndDate = new DateTime(2025, 5, 10),
                    Status = CourseStatus.InProgress,
                    TermId = terms[1].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Course
                {
                    CourseNumber = "ENG102",
                    Title = "Technical Writing",
                    Description = "Professional and technical communication skills",
                    CreditHours = 3,
                    StartDate = new DateTime(2025, 1, 15),
                    EndDate = new DateTime(2025, 5, 10),
                    Status = CourseStatus.InProgress,
                    TermId = terms[1].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                // Summer 2025 courses
                new Course
                {
                    CourseNumber = "CS301",
                    Title = "Database Systems",
                    Description = "Database design, implementation, and management",
                    CreditHours = 3,
                    StartDate = new DateTime(2025, 6, 1),
                    EndDate = new DateTime(2025, 8, 15),
                    Status = CourseStatus.NotStarted,
                    TermId = terms[2].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            context.Courses.AddRange(courses);
            await context.SaveChangesAsync();

            // Create assessments for each course
            var assessments = new List<Assessment>
            {
                // CS101 assessments (completed)
                new Assessment
                {
                    Name = "Programming Assignment 1",
                    Description = "Basic programming fundamentals",
                    Type = AssessmentType.Assignment,
                    DueDate = new DateTime(2024, 9, 30),
                    Status = AssessmentStatus.Completed,
                    Score = 95,
                    MaxPoints = 100,
                    CourseId = courses[0].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Assessment
                {
                    Name = "Midterm Exam",
                    Description = "Comprehensive midterm examination",
                    Type = AssessmentType.Exam,
                    DueDate = new DateTime(2024, 10, 15),
                    Status = AssessmentStatus.Completed,
                    Score = 88,
                    MaxPoints = 100,
                    CourseId = courses[0].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Assessment
                {
                    Name = "Final Project",
                    Description = "Capstone programming project",
                    Type = AssessmentType.Project,
                    DueDate = new DateTime(2024, 12, 10),
                    Status = AssessmentStatus.Completed,
                    Score = 92,
                    MaxPoints = 100,
                    CourseId = courses[0].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // MATH201 assessments (completed)
                new Assessment
                {
                    Name = "Quiz 1 - Limits",
                    Description = "Understanding limits and continuity",
                    Type = AssessmentType.Quiz,
                    DueDate = new DateTime(2024, 9, 20),
                    Status = AssessmentStatus.Completed,
                    Score = 85,
                    MaxPoints = 100,
                    CourseId = courses[1].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Assessment
                {
                    Name = "Midterm Exam",
                    Description = "Derivatives and applications",
                    Type = AssessmentType.Exam,
                    DueDate = new DateTime(2024, 10, 25),
                    Status = AssessmentStatus.Completed,
                    Score = 90,
                    MaxPoints = 100,
                    CourseId = courses[1].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // CS201 assessments (current/upcoming)
                new Assessment
                {
                    Name = "Algorithm Analysis Project",
                    Description = "Analyze time and space complexity of algorithms",
                    Type = AssessmentType.Project,
                    DueDate = DateTime.Now.AddDays(7),
                    Status = AssessmentStatus.InProgress,
                    MaxPoints = 100,
                    CourseId = courses[2].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Assessment
                {
                    Name = "Data Structures Quiz",
                    Description = "Quiz on trees, graphs, and hash tables",
                    Type = AssessmentType.Quiz,
                    DueDate = DateTime.Now.AddDays(3),
                    Status = AssessmentStatus.NotStarted,
                    MaxPoints = 50,
                    CourseId = courses[2].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Assessment
                {
                    Name = "Midterm Exam",
                    Description = "Comprehensive exam covering all topics so far",
                    Type = AssessmentType.Exam,
                    DueDate = DateTime.Now.AddDays(14),
                    Status = AssessmentStatus.NotStarted,
                    MaxPoints = 100,
                    CourseId = courses[2].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // ENG102 assessments (current/upcoming)
                new Assessment
                {
                    Name = "Technical Report",
                    Description = "Write a comprehensive technical report",
                    Type = AssessmentType.Assignment,
                    DueDate = DateTime.Now.AddDays(10),
                    Status = AssessmentStatus.InProgress,
                    MaxPoints = 100,
                    CourseId = courses[3].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Assessment
                {
                    Name = "Presentation",
                    Description = "Present your technical findings",
                    Type = AssessmentType.Performance,
                    DueDate = DateTime.Now.AddDays(21),
                    Status = AssessmentStatus.NotStarted,
                    MaxPoints = 100,
                    CourseId = courses[3].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // Add some overdue assessments for testing
                new Assessment
                {
                    Name = "Overdue Assignment",
                    Description = "This assessment is overdue for testing purposes",
                    Type = AssessmentType.Assignment,
                    DueDate = DateTime.Now.AddDays(-5),
                    Status = AssessmentStatus.NotStarted,
                    MaxPoints = 100,
                    CourseId = courses[2].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },

                // CS301 assessments (future)
                new Assessment
                {
                    Name = "Database Design Project",
                    Description = "Design and implement a database system",
                    Type = AssessmentType.Project,
                    DueDate = new DateTime(2025, 7, 15),
                    Status = AssessmentStatus.NotStarted,
                    MaxPoints = 100,
                    CourseId = courses[4].Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            context.Assessments.AddRange(assessments);
            await context.SaveChangesAsync();

            Console.WriteLine($"Sample data seeded successfully:");
            Console.WriteLine($"- Created {terms.Count} terms");
            Console.WriteLine($"- Created {courses.Count} courses");
            Console.WriteLine($"- Created {assessments.Count} assessments");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error seeding sample data: {ex.Message}");
            throw;
        }
    }

    private static async Task SeedCourseTemplates(ApplicationDbContext context)
    {
        // Check if course templates already exist
        if (await context.CourseTemplates.AnyAsync())
            return;

        var courseTemplates = new List<CourseTemplate>
        {
            new CourseTemplate
            {
                CourseNumber = "CS101",
                Title = "Introduction to Computer Science",
                Description = "Fundamental concepts of computer science and programming",
                CreditHours = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new CourseTemplate
            {
                CourseNumber = "CS201",
                Title = "Data Structures and Algorithms",
                Description = "Advanced programming concepts and algorithm design",
                CreditHours = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new CourseTemplate
            {
                CourseNumber = "MATH201",
                Title = "Calculus I",
                Description = "Introduction to differential and integral calculus",
                CreditHours = 4,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new CourseTemplate
            {
                CourseNumber = "ENG102",
                Title = "Technical Writing",
                Description = "Professional and technical communication skills",
                CreditHours = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new CourseTemplate
            {
                CourseNumber = "CS301",
                Title = "Database Systems",
                Description = "Database design, implementation, and management",
                CreditHours = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        context.CourseTemplates.AddRange(courseTemplates);
        await context.SaveChangesAsync();

        // Now add assessment templates for each course template
        await SeedAssessmentTemplates(context, courseTemplates);
    }

    private static async Task SeedAssessmentTemplates(ApplicationDbContext context, List<CourseTemplate> courseTemplates)
    {
        var assessmentTemplates = new List<AssessmentTemplate>
        {
            // CS101 assessments
            new AssessmentTemplate
            {
                Name = "Programming Assignment 1",
                Description = "Basic programming fundamentals",
                Type = AssessmentType.Assignment,
                MaxPoints = 100,
                DaysFromCourseStart = 30,
                CourseTemplateId = courseTemplates.First(c => c.CourseNumber == "CS101").Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new AssessmentTemplate
            {
                Name = "Midterm Exam",
                Description = "Comprehensive midterm examination",
                Type = AssessmentType.Exam,
                MaxPoints = 100,
                DaysFromCourseStart = 45,
                CourseTemplateId = courseTemplates.First(c => c.CourseNumber == "CS101").Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new AssessmentTemplate
            {
                Name = "Final Project",
                Description = "Capstone programming project",
                Type = AssessmentType.Project,
                MaxPoints = 100,
                DaysFromCourseStart = 100,
                CourseTemplateId = courseTemplates.First(c => c.CourseNumber == "CS101").Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // CS201 assessments
            new AssessmentTemplate
            {
                Name = "Algorithm Analysis Project",
                Description = "Analyze time and space complexity of algorithms",
                Type = AssessmentType.Project,
                MaxPoints = 100,
                DaysFromCourseStart = 21,
                CourseTemplateId = courseTemplates.First(c => c.CourseNumber == "CS201").Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new AssessmentTemplate
            {
                Name = "Data Structures Quiz",
                Description = "Quiz on trees, graphs, and hash tables",
                Type = AssessmentType.Quiz,
                MaxPoints = 50,
                DaysFromCourseStart = 35,
                CourseTemplateId = courseTemplates.First(c => c.CourseNumber == "CS201").Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new AssessmentTemplate
            {
                Name = "Midterm Exam",
                Description = "Comprehensive exam covering all topics so far",
                Type = AssessmentType.Exam,
                MaxPoints = 100,
                DaysFromCourseStart = 50,
                CourseTemplateId = courseTemplates.First(c => c.CourseNumber == "CS201").Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // MATH201 assessments
            new AssessmentTemplate
            {
                Name = "Quiz 1 - Limits",
                Description = "Understanding limits and continuity",
                Type = AssessmentType.Quiz,
                MaxPoints = 100,
                DaysFromCourseStart = 20,
                CourseTemplateId = courseTemplates.First(c => c.CourseNumber == "MATH201").Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new AssessmentTemplate
            {
                Name = "Midterm Exam",
                Description = "Derivatives and applications",
                Type = AssessmentType.Exam,
                MaxPoints = 100,
                DaysFromCourseStart = 55,
                CourseTemplateId = courseTemplates.First(c => c.CourseNumber == "MATH201").Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        context.AssessmentTemplates.AddRange(assessmentTemplates);
        await context.SaveChangesAsync();
    }
}