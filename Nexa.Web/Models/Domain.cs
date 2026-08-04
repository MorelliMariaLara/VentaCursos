namespace Nexa.Web.Models;

public class UserAccount
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "student";
    public string CreatedAt { get; set; } = "";
}

public class Lesson
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public int DurationMinutes { get; set; }
    public string SourceUrl { get; set; } = "";
    public int Order { get; set; }
}

public class CourseModule
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public List<Lesson> Lessons { get; set; } = new();
}

public class Course
{
    public string Id { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "General";
    public string Level { get; set; } = "Inicial";
    public decimal Price { get; set; }
    public string Currency { get; set; } = "ARS";
    public int DurationHours { get; set; } = 1;
    public bool IncludesCertificate { get; set; } = true;
    public string CertificateName { get; set; } = "";
    public string ThumbnailGradient { get; set; } = "linear-gradient(135deg, #0B3D4A 0%, #1A7A6D 55%, #C45C26 100%)";
    public string Instructor { get; set; } = "Equipo NEXA";
    public List<string> LearningOutcomes { get; set; } = new();
    public List<CourseModule> Modules { get; set; } = new();
    public bool Published { get; set; } = true;
    public string? UpdatedAt { get; set; }
}

public class Enrollment
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string CourseId { get; set; } = "";
    public string PurchasedAt { get; set; } = "";
    public Dictionary<string, bool> Progress { get; set; } = new();
    public string? OrderId { get; set; }
    public string? CertificateCode { get; set; }
    public string? CertificateIssuedAt { get; set; }
}

public class Order
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string CourseId { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "ARS";
    public string Status { get; set; } = "pending";
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
    public string? PreferenceId { get; set; }
    public string? PaymentId { get; set; }
    public string? PaymentMethod { get; set; }
    public string? StatusDetail { get; set; }
    public string? PayerEmail { get; set; }
    public bool Simulated { get; set; }
}

public class StoreData
{
    public List<UserAccount> Users { get; set; } = new();
    public List<Course> Courses { get; set; } = new();
    public List<Enrollment> Enrollments { get; set; } = new();
    public List<Order> Orders { get; set; } = new();
}

public class PublicLesson
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public int DurationMinutes { get; set; }
    public int Order { get; set; }
}

public class PublicModule
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public List<PublicLesson> Lessons { get; set; } = new();
}

public class PublicCourse
{
    public string Id { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public string Level { get; set; } = "";
    public decimal Price { get; set; }
    public string Currency { get; set; } = "ARS";
    public int DurationHours { get; set; }
    public bool IncludesCertificate { get; set; }
    public string CertificateName { get; set; } = "";
    public string ThumbnailGradient { get; set; } = "";
    public string Instructor { get; set; } = "";
    public List<string> LearningOutcomes { get; set; } = new();
    public bool Published { get; set; }
    public List<PublicModule> Modules { get; set; } = new();
}

public static class CourseMapper
{
    public static PublicCourse ToPublic(Course course) => new()
    {
        Id = course.Id,
        Slug = course.Slug,
        Title = course.Title,
        Subtitle = course.Subtitle,
        Description = course.Description,
        Category = course.Category,
        Level = course.Level,
        Price = course.Price,
        Currency = course.Currency,
        DurationHours = course.DurationHours,
        IncludesCertificate = course.IncludesCertificate,
        CertificateName = course.CertificateName,
        ThumbnailGradient = course.ThumbnailGradient,
        Instructor = course.Instructor,
        LearningOutcomes = course.LearningOutcomes,
        Published = course.Published,
        Modules = course.Modules.Select(m => new PublicModule
        {
            Id = m.Id,
            Title = m.Title,
            Lessons = m.Lessons.Select(l => new PublicLesson
            {
                Id = l.Id,
                Title = l.Title,
                DurationMinutes = l.DurationMinutes,
                Order = l.Order,
            }).ToList(),
        }).ToList(),
    };

    public static (string ModuleTitle, Lesson Lesson)? FindLesson(Course course, string lessonId)
    {
        foreach (var mod in course.Modules)
        {
            var lesson = mod.Lessons.FirstOrDefault(l => l.Id == lessonId);
            if (lesson != null) return (mod.Title, lesson);
        }
        return null;
    }
}
