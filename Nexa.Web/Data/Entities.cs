namespace Nexa.Web.Data;

public class UserEntity
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "student";
    public DateTime CreatedAt { get; set; }
}

public class CourseEntity
{
    public string Id { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Subtitle { get; set; }
    public string? Description { get; set; }
    public string Category { get; set; } = "General";
    public string Level { get; set; } = "Inicial";
    public decimal Price { get; set; }
    public string Currency { get; set; } = "ARS";
    public int DurationHours { get; set; } = 1;
    public bool IncludesCertificate { get; set; } = true;
    public string? CertificateName { get; set; }
    public string? ThumbnailGradient { get; set; }
    public string? Instructor { get; set; }
    public bool Published { get; set; } = true;
    public DateTime? UpdatedAt { get; set; }

    public List<CourseLearningOutcomeEntity> LearningOutcomes { get; set; } = new();
    public List<CourseModuleEntity> Modules { get; set; } = new();
}

public class CourseLearningOutcomeEntity
{
    public int Id { get; set; }
    public string CourseId { get; set; } = "";
    public int SortOrder { get; set; }
    public string Text { get; set; } = "";
    public CourseEntity? Course { get; set; }
}

public class CourseModuleEntity
{
    public string Id { get; set; } = "";
    public string CourseId { get; set; } = "";
    public string Title { get; set; } = "";
    public int SortOrder { get; set; }
    public CourseEntity? Course { get; set; }
    public List<LessonEntity> Lessons { get; set; } = new();
}

public class LessonEntity
{
    public string Id { get; set; } = "";
    public string ModuleId { get; set; } = "";
    public string Title { get; set; } = "";
    public int DurationMinutes { get; set; }
    public string SourceUrl { get; set; } = "";
    public int Order { get; set; }
    public CourseModuleEntity? Module { get; set; }
}

public class OrderEntity
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string CourseId { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "ARS";
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? PreferenceId { get; set; }
    public string? PaymentId { get; set; }
    public string? PaymentMethod { get; set; }
    public string? StatusDetail { get; set; }
    public string? PayerEmail { get; set; }
    public bool Simulated { get; set; }
}

public class EnrollmentEntity
{
    public string Id { get; set; } = "";
    public string UserId { get; set; } = "";
    public string CourseId { get; set; } = "";
    public DateTime PurchasedAt { get; set; }
    public string? OrderId { get; set; }
    public string? CertificateCode { get; set; }
    public DateTime? CertificateIssuedAt { get; set; }
    public List<EnrollmentProgressEntity> Progress { get; set; } = new();
}

public class EnrollmentProgressEntity
{
    public string EnrollmentId { get; set; } = "";
    public string LessonId { get; set; } = "";
    public bool Completed { get; set; } = true;
    public DateTime CompletedAt { get; set; }
    public EnrollmentEntity? Enrollment { get; set; }
}
