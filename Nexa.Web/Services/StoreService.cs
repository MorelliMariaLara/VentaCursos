using Microsoft.EntityFrameworkCore;
using Nexa.Web.Data;
using Nexa.Web.Models;

namespace Nexa.Web.Services;

public class StoreService
{
    private readonly AppDbContext _db;

    public StoreService(AppDbContext db) => _db = db;

    public const decimal PassPercent = 60m;

    public async Task EnsureSeedAsync()
    {
        await EnsureQuizSchemaAsync();

        if (!await _db.Courses.AnyAsync())
        {
            foreach (var course in CourseCatalog.SeedCourses())
                await InsertCourseGraphAsync(course);
            await _db.SaveChangesAsync();
        }

        await EnsureDemoUsersAsync();
    }

    /// <summary>Crea tablas/columnas de quiz si faltan (SSMS también tiene database/04_QuizTables.sql).</summary>
    public async Task EnsureQuizSchemaAsync()
    {
        var sql = """
            IF COL_LENGTH(N'dbo.EnrollmentProgress', N'VideoWatched') IS NULL
                ALTER TABLE dbo.EnrollmentProgress ADD VideoWatched BIT NOT NULL CONSTRAINT DF_EP_VideoWatched DEFAULT (0);
            IF COL_LENGTH(N'dbo.EnrollmentProgress', N'QuizPassed') IS NULL
                ALTER TABLE dbo.EnrollmentProgress ADD QuizPassed BIT NOT NULL CONSTRAINT DF_EP_QuizPassed DEFAULT (0);

            IF OBJECT_ID(N'dbo.LessonQuestions', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.LessonQuestions (
                    Id NVARCHAR(64) NOT NULL CONSTRAINT PK_LessonQuestions PRIMARY KEY,
                    LessonId NVARCHAR(64) NOT NULL,
                    Prompt NVARCHAR(1000) NOT NULL,
                    SortOrder INT NOT NULL CONSTRAINT DF_LQ_Sort DEFAULT (0),
                    CONSTRAINT FK_LQ_Lessons FOREIGN KEY (LessonId) REFERENCES dbo.Lessons (Id) ON DELETE CASCADE
                );
                CREATE INDEX IX_LessonQuestions_LessonId ON dbo.LessonQuestions (LessonId);
            END

            IF OBJECT_ID(N'dbo.LessonAnswers', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.LessonAnswers (
                    Id NVARCHAR(64) NOT NULL CONSTRAINT PK_LessonAnswers PRIMARY KEY,
                    QuestionId NVARCHAR(64) NOT NULL,
                    Text NVARCHAR(500) NOT NULL,
                    IsCorrect BIT NOT NULL CONSTRAINT DF_LA_Correct DEFAULT (0),
                    SortOrder INT NOT NULL CONSTRAINT DF_LA_Sort DEFAULT (0),
                    CONSTRAINT FK_LA_Questions FOREIGN KEY (QuestionId) REFERENCES dbo.LessonQuestions (Id) ON DELETE CASCADE
                );
                CREATE INDEX IX_LessonAnswers_QuestionId ON dbo.LessonAnswers (QuestionId);
            END

            IF OBJECT_ID(N'dbo.QuizAttempts', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.QuizAttempts (
                    Id NVARCHAR(64) NOT NULL CONSTRAINT PK_QuizAttempts PRIMARY KEY,
                    EnrollmentId NVARCHAR(64) NOT NULL,
                    LessonId NVARCHAR(64) NOT NULL,
                    Score INT NOT NULL,
                    Total INT NOT NULL,
                    PercentScore DECIMAL(5,2) NOT NULL CONSTRAINT DF_QA_Percent DEFAULT (0),
                    Passed BIT NOT NULL CONSTRAINT DF_QA_Passed DEFAULT (0),
                    AttemptedAt DATETIME2(3) NOT NULL CONSTRAINT DF_QA_At DEFAULT (SYSUTCDATETIME()),
                    CONSTRAINT FK_QA_Enrollments FOREIGN KEY (EnrollmentId) REFERENCES dbo.Enrollments (Id) ON DELETE CASCADE
                );
                CREATE INDEX IX_QuizAttempts_Enrollment_Lesson ON dbo.QuizAttempts (EnrollmentId, LessonId);
            END

            IF OBJECT_ID(N'dbo.QuizAttemptAnswers', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.QuizAttemptAnswers (
                    AttemptId NVARCHAR(64) NOT NULL,
                    QuestionId NVARCHAR(64) NOT NULL,
                    AnswerId NVARCHAR(64) NOT NULL,
                    IsCorrect BIT NOT NULL,
                    CONSTRAINT PK_QuizAttemptAnswers PRIMARY KEY (AttemptId, QuestionId),
                    CONSTRAINT FK_QAA_Attempts FOREIGN KEY (AttemptId) REFERENCES dbo.QuizAttempts (Id) ON DELETE CASCADE
                );
            END
            """;
        try { await _db.Database.ExecuteSqlRawAsync(sql); }
        catch (Exception ex) { Console.WriteLine("  AVISO quiz schema: " + ex.Message); }
    }

    private async Task EnsureDemoUsersAsync()
    {
        var demo = await _db.Users.FirstOrDefaultAsync(u =>
            u.Email == "demo@santicaza.com" || u.Email == "demo@nexa.academy");
        var admin = await _db.Users.FirstOrDefaultAsync(u =>
            u.Email == "admin@santicaza.com" || u.Email == "admin@nexa.academy");

        var needs =
            demo == null ||
            admin == null ||
            demo.Email != "demo@santicaza.com" ||
            admin.Email != "admin@santicaza.com" ||
            !PasswordService.Verify("demo1234", demo.PasswordHash) ||
            !PasswordService.Verify("admin1234", admin.PasswordHash);

        if (!needs) return;

        var old = await _db.Users
            .Where(u =>
                u.Email == "demo@santicaza.com" || u.Email == "admin@santicaza.com" ||
                u.Email == "demo@nexa.academy" || u.Email == "admin@nexa.academy")
            .ToListAsync();
        if (old.Count > 0) _db.Users.RemoveRange(old);

        var now = DateTime.UtcNow;
        _db.Users.AddRange(
            new UserEntity
            {
                Id = "user-demo",
                Name = "Estudiante Demo",
                Email = "demo@santicaza.com",
                PasswordHash = PasswordService.Hash("demo1234"),
                Role = "student",
                CreatedAt = now,
            },
            new UserEntity
            {
                Id = "user-admin",
                Name = "Admin SANTICAZA",
                Email = "admin@santicaza.com",
                PasswordHash = PasswordService.Hash("admin1234"),
                Role = "admin",
                CreatedAt = now,
            });
        await _db.SaveChangesAsync();
    }

    private async Task InsertCourseGraphAsync(Course course)
    {
        var entity = new CourseEntity
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
            Published = course.Published,
            UpdatedAt = DateTime.UtcNow,
        };
        _db.Courses.Add(entity);

        var o = 0;
        foreach (var text in course.LearningOutcomes)
        {
            _db.CourseLearningOutcomes.Add(new CourseLearningOutcomeEntity
            {
                CourseId = course.Id,
                SortOrder = ++o,
                Text = text,
            });
        }

        var mi = 0;
        foreach (var mod in course.Modules)
        {
            _db.CourseModules.Add(new CourseModuleEntity
            {
                Id = mod.Id,
                CourseId = course.Id,
                Title = mod.Title,
                SortOrder = ++mi,
            });
            foreach (var les in mod.Lessons)
            {
                _db.Lessons.Add(new LessonEntity
                {
                    Id = les.Id,
                    ModuleId = mod.Id,
                    Title = les.Title,
                    DurationMinutes = les.DurationMinutes,
                    SourceUrl = les.SourceUrl,
                    Order = les.Order,
                });
            }
        }

        await Task.CompletedTask;
    }

    private static Course MapCourse(CourseEntity e) => new()
    {
        Id = e.Id,
        Slug = e.Slug,
        Title = e.Title,
        Subtitle = e.Subtitle ?? "",
        Description = e.Description ?? "",
        Category = e.Category,
        Level = e.Level,
        Price = e.Price,
        Currency = e.Currency,
        DurationHours = e.DurationHours,
        IncludesCertificate = e.IncludesCertificate,
        CertificateName = e.CertificateName ?? "",
        ThumbnailGradient = e.ThumbnailGradient ?? "",
        Instructor = e.Instructor ?? "",
        Published = e.Published,
        UpdatedAt = e.UpdatedAt?.ToString("o"),
        LearningOutcomes = e.LearningOutcomes.OrderBy(x => x.SortOrder).Select(x => x.Text).ToList(),
        Modules = e.Modules.OrderBy(x => x.SortOrder).Select(m => new CourseModule
        {
            Id = m.Id,
            Title = m.Title,
            Lessons = m.Lessons.OrderBy(l => l.Order).Select(l => new Lesson
            {
                Id = l.Id,
                Title = l.Title,
                DurationMinutes = l.DurationMinutes,
                SourceUrl = l.SourceUrl,
                Order = l.Order,
            }).ToList(),
        }).ToList(),
    };

    private static UserAccount MapUser(UserEntity u) => new()
    {
        Id = u.Id,
        Name = u.Name,
        Email = u.Email,
        PasswordHash = u.PasswordHash,
        Role = u.Role,
        CreatedAt = u.CreatedAt.ToString("o"),
    };

    private static Order MapOrder(OrderEntity o) => new()
    {
        Id = o.Id,
        UserId = o.UserId,
        CourseId = o.CourseId,
        Amount = o.Amount,
        Currency = o.Currency,
        Status = o.Status,
        CreatedAt = o.CreatedAt.ToString("o"),
        UpdatedAt = o.UpdatedAt.ToString("o"),
        PreferenceId = o.PreferenceId,
        PaymentId = o.PaymentId,
        PaymentMethod = o.PaymentMethod,
        StatusDetail = o.StatusDetail,
        PayerEmail = o.PayerEmail,
        Simulated = o.Simulated,
    };

    private static Enrollment MapEnrollment(EnrollmentEntity e) => new()
    {
        Id = e.Id,
        UserId = e.UserId,
        CourseId = e.CourseId,
        PurchasedAt = e.PurchasedAt.ToString("o"),
        OrderId = e.OrderId,
        CertificateCode = e.CertificateCode,
        CertificateIssuedAt = e.CertificateIssuedAt?.ToString("o"),
        Progress = e.Progress.ToDictionary(p => p.LessonId, p => p.Completed),
        VideoWatched = e.Progress.ToDictionary(p => p.LessonId, p => p.VideoWatched),
        QuizPassed = e.Progress.ToDictionary(p => p.LessonId, p => p.QuizPassed),
    };

    private IQueryable<CourseEntity> CoursesQuery() =>
        _db.Courses
            .Include(c => c.LearningOutcomes)
            .Include(c => c.Modules)
                .ThenInclude(m => m.Lessons);

    public async Task<List<Course>> ListCoursesAsync(bool includeUnpublished = false)
    {
        var q = CoursesQuery().AsNoTracking();
        if (!includeUnpublished) q = q.Where(c => c.Published);
        var list = await q.OrderBy(c => c.Title).ToListAsync();
        return list.Select(MapCourse).ToList();
    }

    public async Task<Course?> GetCourseBySlugAsync(string slug)
    {
        var e = await CoursesQuery().AsNoTracking().FirstOrDefaultAsync(c => c.Slug == slug);
        return e == null ? null : MapCourse(e);
    }

    public async Task<Course?> GetCourseByIdAsync(string id)
    {
        var e = await CoursesQuery().AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        return e == null ? null : MapCourse(e);
    }

    public async Task<UserAccount?> FindUserByEmailAsync(string email)
    {
        var u = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == email.ToLowerInvariant() || x.Email == email);
        if (u == null)
        {
            u = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Email.ToLower() == email.ToLower());
        }
        return u == null ? null : MapUser(u);
    }

    public async Task<UserAccount?> FindUserByIdAsync(string id)
    {
        var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return u == null ? null : MapUser(u);
    }

    public async Task<List<UserAccount>> ListUsersAsync()
    {
        var users = await _db.Users.AsNoTracking().OrderBy(u => u.Name).ToListAsync();
        return users.Select(MapUser).ToList();
    }

    public async Task<UserAccount> CreateUserAsync(string name, string email, string password, string role = "student")
    {
        var normalized = email.ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == normalized))
            throw new InvalidOperationException("EMAIL_TAKEN");

        var user = new UserEntity
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Email = normalized,
            PasswordHash = PasswordService.Hash(password),
            Role = role,
            CreatedAt = DateTime.UtcNow,
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return MapUser(user);
    }

    public async Task<Enrollment?> GetEnrollmentAsync(string userId, string courseId)
    {
        var e = await _db.Enrollments.AsNoTracking()
            .Include(x => x.Progress)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.CourseId == courseId);
        return e == null ? null : MapEnrollment(e);
    }

    /// <summary>
    /// Acceso al aula: alumno solo si compró; admin se inscribe gratis (sin orden/pago).
    /// </summary>
    public async Task<Enrollment> EnsureCourseAccessAsync(string userId, string courseId, bool isAdmin)
    {
        var existing = await _db.Enrollments
            .Include(x => x.Progress)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.CourseId == courseId);
        if (existing != null) return MapEnrollment(existing);

        if (!isAdmin) throw new InvalidOperationException("NOT_ENROLLED");

        var courseExists = await _db.Courses.AnyAsync(c => c.Id == courseId);
        if (!courseExists) throw new InvalidOperationException("COURSE_NOT_FOUND");

        var enrollment = new EnrollmentEntity
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            CourseId = courseId,
            PurchasedAt = DateTime.UtcNow,
            OrderId = null, // acceso admin sin pago
        };
        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync();
        return MapEnrollment(enrollment);
    }

    public async Task<List<Enrollment>> ListEnrollmentsForUserAsync(string userId)
    {
        var list = await _db.Enrollments.AsNoTracking()
            .Include(x => x.Progress)
            .Where(x => x.UserId == userId)
            .ToListAsync();
        return list.Select(MapEnrollment).ToList();
    }

    public async Task<List<Order>> ListOrdersAsync()
    {
        var list = await _db.Orders.AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
        return list.Select(MapOrder).ToList();
    }

    public async Task<Order?> GetOrderByIdAsync(string id)
    {
        var o = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return o == null ? null : MapOrder(o);
    }

    public async Task<Order> CreatePendingOrderAsync(string userId, string courseId, decimal amount, string currency)
    {
        if (await _db.Orders.AnyAsync(o => o.UserId == userId && o.CourseId == courseId && o.Status == "paid"))
            throw new InvalidOperationException("ALREADY_OWNED");

        // Reutiliza orden pending existente del mismo curso (evita duplicados al refrescar checkout)
        var existing = await _db.Orders
            .Where(o => o.UserId == userId && o.CourseId == courseId && (o.Status == "pending" || o.Status == "in_process"))
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();
        if (existing != null)
        {
            existing.Amount = amount;
            existing.Currency = currency;
            existing.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return MapOrder(existing);
        }

        var now = DateTime.UtcNow;
        var order = new OrderEntity
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            CourseId = courseId,
            Amount = amount,
            Currency = currency,
            Status = "pending",
            CreatedAt = now,
            UpdatedAt = now,
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        return MapOrder(order);
    }

    public async Task<Order> UpdateOrderAsync(string orderId, Action<Order> patch)
    {
        var entity = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new InvalidOperationException("ORDER_NOT_FOUND");

        var mapped = MapOrder(entity);
        patch(mapped);

        entity.Status = mapped.Status;
        entity.PreferenceId = mapped.PreferenceId;
        entity.PaymentId = mapped.PaymentId;
        entity.PaymentMethod = mapped.PaymentMethod;
        entity.StatusDetail = mapped.StatusDetail;
        entity.PayerEmail = mapped.PayerEmail;
        entity.Simulated = mapped.Simulated;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapOrder(entity);
    }

    public async Task<(Order Order, Enrollment Enrollment)> FulfillPaidOrderAsync(string orderId)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new InvalidOperationException("ORDER_NOT_FOUND");

        order.Status = "paid";
        order.UpdatedAt = DateTime.UtcNow;

        var enrollment = await _db.Enrollments
            .Include(e => e.Progress)
            .FirstOrDefaultAsync(e => e.UserId == order.UserId && e.CourseId == order.CourseId);

        if (enrollment == null)
        {
            enrollment = new EnrollmentEntity
            {
                Id = Guid.NewGuid().ToString(),
                UserId = order.UserId,
                CourseId = order.CourseId,
                PurchasedAt = DateTime.UtcNow,
                OrderId = order.Id,
            };
            _db.Enrollments.Add(enrollment);
        }
        else
        {
            enrollment.OrderId = order.Id;
        }

        await _db.SaveChangesAsync();
        return (MapOrder(order), MapEnrollment(enrollment));
    }

    public async Task<Enrollment> MarkLessonCompleteAsync(string userId, string courseId, string lessonId)
    {
        var enrollment = await _db.Enrollments
            .Include(e => e.Progress)
            .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId)
            ?? throw new InvalidOperationException("NOT_ENROLLED");

        var questionCount = await _db.LessonQuestions.CountAsync(q => q.LessonId == lessonId);
        var progress = await GetOrCreateProgressAsync(enrollment, lessonId);

        if (questionCount > 0 && !progress.QuizPassed)
            throw new InvalidOperationException("QUIZ_REQUIRED");

        if (questionCount == 0 && !progress.VideoWatched)
            throw new InvalidOperationException("VIDEO_REQUIRED");

        progress.Completed = true;
        progress.CompletedAt = DateTime.UtcNow;
        await TryIssueCertificateAsync(enrollment, courseId);
        await _db.SaveChangesAsync();
        return MapEnrollment(enrollment);
    }

    private async Task<EnrollmentProgressEntity> GetOrCreateProgressAsync(EnrollmentEntity enrollment, string lessonId)
    {
        var progress = enrollment.Progress.FirstOrDefault(p => p.LessonId == lessonId);
        if (progress != null) return progress;
        progress = new EnrollmentProgressEntity
        {
            EnrollmentId = enrollment.Id,
            LessonId = lessonId,
            Completed = false,
            VideoWatched = false,
            QuizPassed = false,
            CompletedAt = DateTime.UtcNow,
        };
        enrollment.Progress.Add(progress);
        await _db.SaveChangesAsync();
        return progress;
    }

    public async Task<Enrollment> MarkVideoWatchedAsync(string userId, string courseId, string lessonId)
    {
        var enrollment = await _db.Enrollments
            .Include(e => e.Progress)
            .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId)
            ?? throw new InvalidOperationException("NOT_ENROLLED");

        var found = CourseMapper.FindLesson(
            (await GetCourseByIdAsync(courseId)) ?? throw new InvalidOperationException("COURSE_NOT_FOUND"),
            lessonId);
        if (found == null) throw new InvalidOperationException("LESSON_NOT_FOUND");

        var progress = await GetOrCreateProgressAsync(enrollment, lessonId);
        progress.VideoWatched = true;

        // Sin preguntas: al terminar el video se completa la lección
        var qCount = await _db.LessonQuestions.CountAsync(q => q.LessonId == lessonId);
        if (qCount == 0)
        {
            progress.Completed = true;
            progress.QuizPassed = true;
            progress.CompletedAt = DateTime.UtcNow;
            await TryIssueCertificateAsync(enrollment, courseId);
        }

        await _db.SaveChangesAsync();
        return MapEnrollment(enrollment);
    }

    public async Task<List<LessonQuestion>> ListQuestionsForLessonAsync(string lessonId, bool includeCorrect)
    {
        var questions = await _db.LessonQuestions.AsNoTracking()
            .Include(q => q.Answers)
            .Where(q => q.LessonId == lessonId)
            .OrderBy(q => q.SortOrder)
            .ToListAsync();

        return questions.Select(q => new LessonQuestion
        {
            Id = q.Id,
            LessonId = q.LessonId,
            Prompt = q.Prompt,
            SortOrder = q.SortOrder,
            Answers = q.Answers.OrderBy(a => a.SortOrder).Select(a => new LessonAnswerOption
            {
                Id = a.Id,
                Text = a.Text,
                SortOrder = a.SortOrder,
                IsCorrect = includeCorrect && a.IsCorrect,
            }).ToList(),
        }).ToList();
    }

    public async Task<LessonQuestion> AddQuestionAsync(
        string lessonId, string prompt, IReadOnlyList<(string Text, bool IsCorrect)> options)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new InvalidOperationException("La pregunta no puede estar vacía.");
        if (options.Count < 2)
            throw new InvalidOperationException("Cada pregunta necesita al menos 2 opciones.");
        if (options.Count(o => o.IsCorrect) != 1)
            throw new InvalidOperationException("Marcá exactamente una opción correcta.");

        var exists = await _db.Lessons.AnyAsync(l => l.Id == lessonId);
        if (!exists) throw new InvalidOperationException("LESSON_NOT_FOUND");

        var sort = await _db.LessonQuestions.CountAsync(q => q.LessonId == lessonId) + 1;
        var q = new LessonQuestionEntity
        {
            Id = Guid.NewGuid().ToString(),
            LessonId = lessonId,
            Prompt = prompt.Trim(),
            SortOrder = sort,
        };
        _db.LessonQuestions.Add(q);
        var i = 0;
        foreach (var opt in options)
        {
            _db.LessonAnswers.Add(new LessonAnswerEntity
            {
                Id = Guid.NewGuid().ToString(),
                QuestionId = q.Id,
                Text = opt.Text.Trim(),
                IsCorrect = opt.IsCorrect,
                SortOrder = ++i,
            });
        }
        await _db.SaveChangesAsync();
        return (await ListQuestionsForLessonAsync(lessonId, true)).First(x => x.Id == q.Id);
    }

    public async Task DeleteQuestionAsync(string questionId)
    {
        var q = await _db.LessonQuestions.Include(x => x.Answers)
            .FirstOrDefaultAsync(x => x.Id == questionId)
            ?? throw new InvalidOperationException("QUESTION_NOT_FOUND");
        _db.LessonAnswers.RemoveRange(q.Answers);
        _db.LessonQuestions.Remove(q);
        await _db.SaveChangesAsync();
    }

    public async Task<QuizSubmitResult> SubmitLessonQuizAsync(
        string userId, string courseId, string lessonId, Dictionary<string, string> answersByQuestionId)
    {
        var enrollment = await _db.Enrollments
            .Include(e => e.Progress)
            .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId)
            ?? throw new InvalidOperationException("NOT_ENROLLED");

        var progress = await GetOrCreateProgressAsync(enrollment, lessonId);
        if (!progress.VideoWatched)
            throw new InvalidOperationException("VIDEO_REQUIRED");

        var questions = await _db.LessonQuestions
            .Include(q => q.Answers)
            .Where(q => q.LessonId == lessonId)
            .OrderBy(q => q.SortOrder)
            .ToListAsync();

        if (questions.Count == 0)
            throw new InvalidOperationException("NO_QUESTIONS");

        var score = 0;
        var attempt = new QuizAttemptEntity
        {
            Id = Guid.NewGuid().ToString(),
            EnrollmentId = enrollment.Id,
            LessonId = lessonId,
            Total = questions.Count,
            AttemptedAt = DateTime.UtcNow,
        };

        foreach (var q in questions)
        {
            answersByQuestionId.TryGetValue(q.Id, out var chosenId);
            var chosen = q.Answers.FirstOrDefault(a => a.Id == chosenId);
            var correct = chosen != null && chosen.IsCorrect;
            if (correct) score++;
            attempt.Answers.Add(new QuizAttemptAnswerEntity
            {
                AttemptId = attempt.Id,
                QuestionId = q.Id,
                AnswerId = chosen?.Id ?? "",
                IsCorrect = correct,
            });
        }

        attempt.Score = score;
        attempt.PercentScore = Math.Round(100m * score / questions.Count, 2);
        attempt.Passed = attempt.PercentScore >= PassPercent; // info por lección
        _db.QuizAttempts.Add(attempt);

        // La lección queda respondida; la aprobación del curso es por promedio global ≥ 60%
        progress.QuizPassed = true;
        progress.Completed = true;
        progress.CompletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var course = await GetCourseByIdAsync(courseId)
            ?? throw new InvalidOperationException("COURSE_NOT_FOUND");
        var courseStats = await ComputeCourseQuizStatsAsync(enrollment.Id, course);
        var mustRestart = false;
        var courseApproved = false;
        var message =
            $"Respuestas de esta lección: {score}/{questions.Count} ({attempt.PercentScore:0.#}%). " +
            $"Promedio del curso hasta ahora: {courseStats.CoursePercent:0.#}%.";

        // Cuando contestó todas las lecciones con preguntas → aprobar o reiniciar todo
        if (courseStats.AllQuizzesDone)
        {
            if (courseStats.CoursePercent >= PassPercent)
            {
                courseApproved = true;
                await TryIssueCertificateAsync(enrollment, courseId);
                await _db.SaveChangesAsync();
                message = $"Curso aprobado con {courseStats.CoursePercent:0.#}% de respuestas correctas.";
            }
            else
            {
                mustRestart = true;
                await ResetEnrollmentLearningAsync(enrollment);
                message =
                    $"Promedio del curso: {courseStats.CoursePercent:0.#}% (mínimo {PassPercent:0}%). " +
                    "Debés hacer todo de nuevo: ver cada video completo y responder las preguntas.";
            }
        }

        enrollment = (await _db.Enrollments.Include(e => e.Progress)
            .FirstAsync(e => e.Id == enrollment.Id));

        return new QuizSubmitResult
        {
            Score = score,
            Total = questions.Count,
            LessonPercent = attempt.PercentScore,
            LessonPassed = attempt.Passed,
            CoursePercent = courseStats.CoursePercent,
            CourseApproved = courseApproved,
            MustRestart = mustRestart,
            Enrollment = MapEnrollment(enrollment),
            Message = message,
        };
    }

    private async Task<(decimal CoursePercent, bool AllQuizzesDone)> ComputeCourseQuizStatsAsync(
        string enrollmentId, Course course)
    {
        var lessonIds = course.Modules.SelectMany(m => m.Lessons).Select(l => l.Id).ToList();
        var lessonsWithQuestions = await _db.LessonQuestions.AsNoTracking()
            .Where(q => lessonIds.Contains(q.LessonId))
            .GroupBy(q => q.LessonId)
            .Select(g => g.Key)
            .ToListAsync();

        if (lessonsWithQuestions.Count == 0)
            return (100m, true);

        // Último intento por lección
        var attempts = await _db.QuizAttempts.AsNoTracking()
            .Where(a => a.EnrollmentId == enrollmentId && lessonsWithQuestions.Contains(a.LessonId))
            .OrderByDescending(a => a.AttemptedAt)
            .ToListAsync();

        var latest = attempts
            .GroupBy(a => a.LessonId)
            .ToDictionary(g => g.Key, g => g.First());

        var allDone = lessonsWithQuestions.All(latest.ContainsKey);
        var totalScore = latest.Values.Sum(a => a.Score);
        var totalQ = latest.Values.Sum(a => a.Total);
        var pct = totalQ == 0 ? 0m : Math.Round(100m * totalScore / totalQ, 2);
        return (pct, allDone);
    }

    private async Task ResetEnrollmentLearningAsync(EnrollmentEntity enrollment)
    {
        var attempts = await _db.QuizAttempts
            .Include(a => a.Answers)
            .Where(a => a.EnrollmentId == enrollment.Id)
            .ToListAsync();
        _db.QuizAttemptAnswers.RemoveRange(attempts.SelectMany(a => a.Answers));
        _db.QuizAttempts.RemoveRange(attempts);
        _db.EnrollmentProgress.RemoveRange(enrollment.Progress);
        enrollment.Progress.Clear();
        enrollment.CertificateCode = null;
        enrollment.CertificateIssuedAt = null;
        await _db.SaveChangesAsync();
    }

    private async Task TryIssueCertificateAsync(EnrollmentEntity enrollment, string courseId)
    {
        if (enrollment.CertificateIssuedAt != null) return;
        var course = await CoursesQuery().AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId);
        if (course?.IncludesCertificate != true) return;

        var allLessonIds = course.Modules.SelectMany(m => m.Lessons).Select(l => l.Id).ToList();
        if (allLessonIds.Count == 0) return;

        // Recargar progress
        await _db.Entry(enrollment).Collection(e => e.Progress).LoadAsync();
        var done = enrollment.Progress.Where(p => p.Completed).Select(p => p.LessonId).ToHashSet();
        if (!allLessonIds.All(done.Contains)) return;

        var stats = await ComputeCourseQuizStatsAsync(enrollment.Id, MapCourse(course));
        if (!stats.AllQuizzesDone || stats.CoursePercent < PassPercent) return;

        enrollment.CertificateIssuedAt = DateTime.UtcNow;
        var prefix = course.Slug.Length >= 4
            ? course.Slug[..4].ToUpperInvariant()
            : course.Slug.ToUpperInvariant();
        enrollment.CertificateCode =
            $"SCZ-{prefix}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
    }

    public async Task<Course> UpsertCourseAsync(Course input)
    {
        if (!string.IsNullOrEmpty(input.Id))
        {
            var existing = await CoursesQuery().FirstOrDefaultAsync(c => c.Id == input.Id)
                ?? throw new InvalidOperationException("COURSE_NOT_FOUND");
            if (await _db.Courses.AnyAsync(c => c.Slug == input.Slug && c.Id != input.Id))
                throw new InvalidOperationException("SLUG_TAKEN");

            existing.Slug = input.Slug;
            existing.Title = input.Title;
            existing.Subtitle = input.Subtitle;
            existing.Description = input.Description;
            existing.Category = input.Category;
            existing.Level = input.Level;
            existing.Price = input.Price;
            existing.Currency = input.Currency;
            existing.DurationHours = input.DurationHours;
            existing.IncludesCertificate = input.IncludesCertificate;
            existing.CertificateName = input.CertificateName;
            existing.ThumbnailGradient = input.ThumbnailGradient;
            existing.Instructor = input.Instructor;
            existing.Published = input.Published;
            existing.UpdatedAt = DateTime.UtcNow;

            _db.CourseLearningOutcomes.RemoveRange(existing.LearningOutcomes);
            _db.Lessons.RemoveRange(existing.Modules.SelectMany(m => m.Lessons));
            _db.CourseModules.RemoveRange(existing.Modules);
            await _db.SaveChangesAsync();

            // reload clean and re-add children
            existing = await _db.Courses.FirstAsync(c => c.Id == input.Id);
            var o = 0;
            foreach (var text in input.LearningOutcomes)
            {
                _db.CourseLearningOutcomes.Add(new CourseLearningOutcomeEntity
                {
                    CourseId = existing.Id,
                    SortOrder = ++o,
                    Text = text,
                });
            }
            var mi = 0;
            foreach (var mod in input.Modules)
            {
                var modId = string.IsNullOrEmpty(mod.Id) ? Guid.NewGuid().ToString() : mod.Id;
                _db.CourseModules.Add(new CourseModuleEntity
                {
                    Id = modId,
                    CourseId = existing.Id,
                    Title = mod.Title,
                    SortOrder = ++mi,
                });
                foreach (var les in mod.Lessons)
                {
                    _db.Lessons.Add(new LessonEntity
                    {
                        Id = string.IsNullOrEmpty(les.Id) ? Guid.NewGuid().ToString() : les.Id,
                        ModuleId = modId,
                        Title = les.Title,
                        DurationMinutes = les.DurationMinutes,
                        SourceUrl = les.SourceUrl,
                        Order = les.Order,
                    });
                }
            }
            await _db.SaveChangesAsync();
            return (await GetCourseByIdAsync(existing.Id))!;
        }

        if (await _db.Courses.AnyAsync(c => c.Slug == input.Slug))
            throw new InvalidOperationException("SLUG_TAKEN");

        input.Id = Guid.NewGuid().ToString();
        if (string.IsNullOrEmpty(input.CertificateName))
            input.CertificateName = $"Certificación SANTICAZA en {input.Title}";
        input.Published = input.Published;
        await InsertCourseGraphAsync(input);
        await _db.SaveChangesAsync();
        return (await GetCourseByIdAsync(input.Id))!;
    }

    public async Task DeleteCourseAsync(string id)
    {
        var course = await CoursesQuery().FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new InvalidOperationException("COURSE_NOT_FOUND");

        _db.Lessons.RemoveRange(course.Modules.SelectMany(m => m.Lessons));
        _db.CourseModules.RemoveRange(course.Modules);
        _db.CourseLearningOutcomes.RemoveRange(course.LearningOutcomes);
        _db.Courses.Remove(course);
        await _db.SaveChangesAsync();
    }

    public async Task<CourseModule> AddModuleAsync(string courseId, string title)
    {
        var course = await CoursesQuery().FirstOrDefaultAsync(c => c.Id == courseId)
            ?? throw new InvalidOperationException("COURSE_NOT_FOUND");
        var mod = new CourseModuleEntity
        {
            Id = Guid.NewGuid().ToString(),
            CourseId = course.Id,
            Title = title.Trim(),
            SortOrder = course.Modules.Count + 1,
        };
        _db.CourseModules.Add(mod);
        await _db.SaveChangesAsync();
        return new CourseModule { Id = mod.Id, Title = mod.Title, Lessons = new() };
    }

    public async Task DeleteModuleAsync(string moduleId)
    {
        var mod = await _db.CourseModules.Include(m => m.Lessons)
            .FirstOrDefaultAsync(m => m.Id == moduleId)
            ?? throw new InvalidOperationException("MODULE_NOT_FOUND");
        _db.Lessons.RemoveRange(mod.Lessons);
        _db.CourseModules.Remove(mod);
        await _db.SaveChangesAsync();
    }

    public async Task<Lesson> AddLessonAsync(
        string moduleId, string title, int durationMinutes, string sourceUrl)
    {
        var mod = await _db.CourseModules.Include(m => m.Lessons)
            .FirstOrDefaultAsync(m => m.Id == moduleId)
            ?? throw new InvalidOperationException("MODULE_NOT_FOUND");

        var normalized = VideoSources.Normalize(sourceUrl);
        var lesson = new LessonEntity
        {
            Id = Guid.NewGuid().ToString(),
            ModuleId = mod.Id,
            Title = title.Trim(),
            DurationMinutes = Math.Max(1, durationMinutes),
            SourceUrl = normalized,
            Order = mod.Lessons.Count + 1,
        };
        _db.Lessons.Add(lesson);
        await _db.SaveChangesAsync();
        return new Lesson
        {
            Id = lesson.Id,
            Title = lesson.Title,
            DurationMinutes = lesson.DurationMinutes,
            SourceUrl = lesson.SourceUrl,
            Order = lesson.Order,
        };
    }

    public async Task UpdateLessonAsync(
        string lessonId, string title, int durationMinutes, string? sourceUrlOrNullKeep)
    {
        var lesson = await _db.Lessons.FirstOrDefaultAsync(l => l.Id == lessonId)
            ?? throw new InvalidOperationException("LESSON_NOT_FOUND");
        lesson.Title = title.Trim();
        lesson.DurationMinutes = Math.Max(1, durationMinutes);
        if (!string.IsNullOrWhiteSpace(sourceUrlOrNullKeep))
            lesson.SourceUrl = VideoSources.Normalize(sourceUrlOrNullKeep);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteLessonAsync(string lessonId)
    {
        var lesson = await _db.Lessons.FirstOrDefaultAsync(l => l.Id == lessonId)
            ?? throw new InvalidOperationException("LESSON_NOT_FOUND");
        _db.Lessons.Remove(lesson);
        await _db.SaveChangesAsync();
    }

    public async Task<StoreStats> StatsAsync()
    {
        var paid = await _db.Orders.AsNoTracking().Where(o => o.Status == "paid").ToListAsync();
        return new StoreStats(
            await _db.Users.CountAsync(),
            await _db.Courses.CountAsync(),
            await _db.Enrollments.CountAsync(),
            await _db.Orders.CountAsync(),
            paid.Sum(o => o.Amount));
    }
}

public record StoreStats(int Users, int Courses, int Enrollments, int Orders, decimal Revenue);
