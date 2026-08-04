using Microsoft.EntityFrameworkCore;
using Nexa.Web.Data;
using Nexa.Web.Models;

namespace Nexa.Web.Services;

public class StoreService
{
    private readonly AppDbContext _db;

    public StoreService(AppDbContext db) => _db = db;

    public async Task EnsureSeedAsync()
    {
        if (!await _db.Courses.AnyAsync())
        {
            foreach (var course in CourseCatalog.SeedCourses())
                await InsertCourseGraphAsync(course);
            await _db.SaveChangesAsync();
        }

        await EnsureDemoUsersAsync();
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

        var progress = enrollment.Progress.FirstOrDefault(p => p.LessonId == lessonId);
        if (progress == null)
        {
            enrollment.Progress.Add(new EnrollmentProgressEntity
            {
                EnrollmentId = enrollment.Id,
                LessonId = lessonId,
                Completed = true,
                CompletedAt = DateTime.UtcNow,
            });
        }
        else
        {
            progress.Completed = true;
            progress.CompletedAt = DateTime.UtcNow;
        }

        if (enrollment.CertificateIssuedAt == null)
        {
            var course = await CoursesQuery().AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId);
            if (course?.IncludesCertificate == true)
            {
                var allLessonIds = course.Modules.SelectMany(m => m.Lessons).Select(l => l.Id).ToList();
                var done = enrollment.Progress
                    .Where(p => p.Completed)
                    .Select(p => p.LessonId)
                    .Append(lessonId)
                    .Distinct()
                    .ToHashSet();
                if (allLessonIds.Count > 0 && allLessonIds.All(done.Contains))
                {
                    enrollment.CertificateIssuedAt = DateTime.UtcNow;
                    var prefix = course.Slug.Length >= 4
                        ? course.Slug[..4].ToUpperInvariant()
                        : course.Slug.ToUpperInvariant();
                    enrollment.CertificateCode =
                        $"SCZ-{prefix}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
                }
            }
        }

        await _db.SaveChangesAsync();
        return MapEnrollment(enrollment);
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
