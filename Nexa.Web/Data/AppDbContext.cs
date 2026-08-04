using Microsoft.EntityFrameworkCore;

namespace Nexa.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<CourseEntity> Courses => Set<CourseEntity>();
    public DbSet<CourseLearningOutcomeEntity> CourseLearningOutcomes => Set<CourseLearningOutcomeEntity>();
    public DbSet<CourseModuleEntity> CourseModules => Set<CourseModuleEntity>();
    public DbSet<LessonEntity> Lessons => Set<LessonEntity>();
    public DbSet<OrderEntity> Orders => Set<OrderEntity>();
    public DbSet<EnrollmentEntity> Enrollments => Set<EnrollmentEntity>();
    public DbSet<EnrollmentProgressEntity> EnrollmentProgress => Set<EnrollmentProgressEntity>();
    public DbSet<LessonQuestionEntity> LessonQuestions => Set<LessonQuestionEntity>();
    public DbSet<LessonAnswerEntity> LessonAnswers => Set<LessonAnswerEntity>();
    public DbSet<QuizAttemptEntity> QuizAttempts => Set<QuizAttemptEntity>();
    public DbSet<QuizAttemptAnswerEntity> QuizAttemptAnswers => Set<QuizAttemptAnswerEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(64);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            e.Property(x => x.Role).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<CourseEntity>(e =>
        {
            e.ToTable("Courses");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(64);
            e.Property(x => x.Slug).HasMaxLength(160).IsRequired();
            e.Property(x => x.Title).HasMaxLength(300).IsRequired();
            e.Property(x => x.Subtitle).HasMaxLength(500);
            e.Property(x => x.Category).HasMaxLength(100);
            e.Property(x => x.Level).HasMaxLength(50);
            e.Property(x => x.Price).HasColumnType("decimal(18,2)");
            e.Property(x => x.Currency).HasMaxLength(10);
            e.Property(x => x.CertificateName).HasMaxLength(300);
            e.Property(x => x.ThumbnailGradient).HasMaxLength(500);
            e.Property(x => x.Instructor).HasMaxLength(200);
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasMany(x => x.LearningOutcomes).WithOne(x => x.Course!).HasForeignKey(x => x.CourseId);
            e.HasMany(x => x.Modules).WithOne(x => x.Course!).HasForeignKey(x => x.CourseId);
        });

        modelBuilder.Entity<CourseLearningOutcomeEntity>(e =>
        {
            e.ToTable("CourseLearningOutcomes");
            e.HasKey(x => x.Id);
            e.Property(x => x.CourseId).HasMaxLength(64);
            e.Property(x => x.Text).HasMaxLength(500).IsRequired();
        });

        modelBuilder.Entity<CourseModuleEntity>(e =>
        {
            e.ToTable("CourseModules");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(64);
            e.Property(x => x.CourseId).HasMaxLength(64);
            e.Property(x => x.Title).HasMaxLength(300).IsRequired();
            e.HasMany(x => x.Lessons).WithOne(x => x.Module!).HasForeignKey(x => x.ModuleId);
        });

        modelBuilder.Entity<LessonEntity>(e =>
        {
            e.ToTable("Lessons");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(64);
            e.Property(x => x.ModuleId).HasMaxLength(64);
            e.Property(x => x.Title).HasMaxLength(300).IsRequired();
            e.Property(x => x.SourceUrl).HasMaxLength(500).IsRequired();
            e.Property(x => x.Order).HasColumnName("Order");
            e.HasMany(x => x.Questions).WithOne(x => x.Lesson!).HasForeignKey(x => x.LessonId);
        });

        modelBuilder.Entity<OrderEntity>(e =>
        {
            e.ToTable("Orders");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(64);
            e.Property(x => x.UserId).HasMaxLength(64);
            e.Property(x => x.CourseId).HasMaxLength(64);
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            e.Property(x => x.Currency).HasMaxLength(10);
            e.Property(x => x.Status).HasMaxLength(50);
            e.Property(x => x.PreferenceId).HasMaxLength(100);
            e.Property(x => x.PaymentId).HasMaxLength(100);
            e.Property(x => x.PaymentMethod).HasMaxLength(100);
            e.Property(x => x.StatusDetail).HasMaxLength(300);
            e.Property(x => x.PayerEmail).HasMaxLength(256);
        });

        modelBuilder.Entity<EnrollmentEntity>(e =>
        {
            e.ToTable("Enrollments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(64);
            e.Property(x => x.UserId).HasMaxLength(64);
            e.Property(x => x.CourseId).HasMaxLength(64);
            e.Property(x => x.OrderId).HasMaxLength(64);
            e.Property(x => x.CertificateCode).HasMaxLength(100);
            e.HasIndex(x => new { x.UserId, x.CourseId }).IsUnique();
            e.HasMany(x => x.Progress).WithOne(x => x.Enrollment!).HasForeignKey(x => x.EnrollmentId);
        });

        modelBuilder.Entity<EnrollmentProgressEntity>(e =>
        {
            e.ToTable("EnrollmentProgress");
            e.HasKey(x => new { x.EnrollmentId, x.LessonId });
            e.Property(x => x.EnrollmentId).HasMaxLength(64);
            e.Property(x => x.LessonId).HasMaxLength(64);
        });

        modelBuilder.Entity<LessonQuestionEntity>(e =>
        {
            e.ToTable("LessonQuestions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(64);
            e.Property(x => x.LessonId).HasMaxLength(64);
            e.Property(x => x.Prompt).HasMaxLength(1000).IsRequired();
            e.HasMany(x => x.Answers).WithOne(x => x.Question!).HasForeignKey(x => x.QuestionId);
        });

        modelBuilder.Entity<LessonAnswerEntity>(e =>
        {
            e.ToTable("LessonAnswers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(64);
            e.Property(x => x.QuestionId).HasMaxLength(64);
            e.Property(x => x.Text).HasMaxLength(500).IsRequired();
        });

        modelBuilder.Entity<QuizAttemptEntity>(e =>
        {
            e.ToTable("QuizAttempts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(64);
            e.Property(x => x.EnrollmentId).HasMaxLength(64);
            e.Property(x => x.LessonId).HasMaxLength(64);
            e.Property(x => x.PercentScore).HasColumnType("decimal(5,2)");
            e.HasMany(x => x.Answers).WithOne(x => x.Attempt!).HasForeignKey(x => x.AttemptId);
        });

        modelBuilder.Entity<QuizAttemptAnswerEntity>(e =>
        {
            e.ToTable("QuizAttemptAnswers");
            e.HasKey(x => new { x.AttemptId, x.QuestionId });
            e.Property(x => x.AttemptId).HasMaxLength(64);
            e.Property(x => x.QuestionId).HasMaxLength(64);
            e.Property(x => x.AnswerId).HasMaxLength(64);
        });
    }
}
