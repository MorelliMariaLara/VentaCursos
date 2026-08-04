using System.ComponentModel.DataAnnotations;

namespace Nexa.Web.Models;

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = "demo@santicaza.com";

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = "demo1234";

    public string? Error { get; set; }
}

public class RegisterViewModel
{
    [Required, Display(Name = "Nombre")]
    public string Name { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, MinLength(6), DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public string? Error { get; set; }
}

public class CourseDetailViewModel
{
    public PublicCourse Course { get; set; } = new();
    public bool Enrolled { get; set; }
    public Enrollment? Enrollment { get; set; }
    public string PriceLabel { get; set; } = "";
}

public class MyCourseItemViewModel
{
    public PublicCourse Course { get; set; } = new();
    public Enrollment Enrollment { get; set; } = new();
}

public class CheckoutViewModel
{
    public PublicCourse Course { get; set; } = new();
    public string PriceLabel { get; set; } = "";
    public string? Status { get; set; }
}

public class LearnViewModel
{
    public PublicCourse Course { get; set; } = new();
    public Enrollment Enrollment { get; set; } = new();
}

public class CertificateViewModel
{
    public PublicCourse Course { get; set; } = new();
    public string CertificateCode { get; set; } = "";
    public string IssuedAt { get; set; } = "";
    public string StudentName { get; set; } = "";
}

public class AdminCourseForm
{
    [Required] public string Title { get; set; } = "";
    [Required] public string Slug { get; set; } = "";
    [Required] public decimal Price { get; set; } = 50000;
    public string Description { get; set; } = "";
}

public class AdminDashboardViewModel
{
    public int Users { get; set; }
    public int Courses { get; set; }
    public int Enrollments { get; set; }
    public int Orders { get; set; }
    public decimal Revenue { get; set; }
    public List<Order> RecentOrders { get; set; } = new();
    public List<UserAccount> UserList { get; set; } = new();
    public List<PublicCourse> CourseList { get; set; } = new();
    public AdminCourseForm NewCourse { get; set; } = new();
    public string? Error { get; set; }
    public string? Message { get; set; }
}

public class AdminCourseEditViewModel
{
    public Course Course { get; set; } = new();
    public Dictionary<string, List<LessonQuestion>> QuestionsByLesson { get; set; } = new();
    public string? Error { get; set; }
    public string? Message { get; set; }
}

public class AdminModuleForm
{
    [Required] public string CourseId { get; set; } = "";
    [Required] public string Title { get; set; } = "Módulo 1";
}

public class AdminLessonForm
{
    [Required] public string CourseId { get; set; } = "";
    [Required] public string ModuleId { get; set; } = "";
    [Required] public string Title { get; set; } = "";
    public int DurationMinutes { get; set; } = 10;
    /// <summary>youtube | upload</summary>
    [Required] public string SourceType { get; set; } = "youtube";
    public string? YoutubeUrl { get; set; }
}

public class AdminQuestionForm
{
    [Required] public string CourseId { get; set; } = "";
    [Required] public string LessonId { get; set; } = "";
    [Required] public string Prompt { get; set; } = "";
    [Required] public string OptionA { get; set; } = "";
    [Required] public string OptionB { get; set; } = "";
    public string? OptionC { get; set; }
    public string? OptionD { get; set; }
    /// <summary>A, B, C o D</summary>
    [Required] public string CorrectOption { get; set; } = "A";
}
