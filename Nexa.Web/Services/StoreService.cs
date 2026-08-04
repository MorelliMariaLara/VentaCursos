using System.Text.Json;
using Nexa.Web.Models;

namespace Nexa.Web.Services;

public class StoreService
{
    private readonly string _dbPath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public StoreService(IWebHostEnvironment env, IConfiguration config)
    {
        var configured = config["DataPath"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            _dbPath = Path.GetFullPath(Path.Combine(env.ContentRootPath, configured));
        }
        else
        {
            // data/ junto a la solución (padre de Nexa.Web)
            var solutionRoot = Directory.GetParent(env.ContentRootPath)?.FullName ?? env.ContentRootPath;
            _dbPath = Path.Combine(solutionRoot, "data", "store.json");
        }
    }

    private async Task<StoreData> ReadAsync()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
        if (!File.Exists(_dbPath))
        {
            var seed = await CreateSeedAsync();
            await WriteUnlockedAsync(seed);
            return seed;
        }

        await using var fs = File.OpenRead(_dbPath);
        var db = await JsonSerializer.DeserializeAsync<StoreData>(fs, JsonOpts) ?? new StoreData();
        db.Users ??= new();
        db.Courses ??= new();
        db.Enrollments ??= new();
        db.Orders ??= new();

        var dirty = false;
        if (db.Courses.Count == 0)
        {
            db.Courses = CourseCatalog.SeedCourses();
            dirty = true;
        }
        else if (db.Courses.Any(c =>
                     (c.CertificateName?.Contains("NEXA", StringComparison.OrdinalIgnoreCase) ?? false) ||
                     (c.Instructor?.Contains("NEXA", StringComparison.OrdinalIgnoreCase) ?? false)))
        {
            // Actualiza branding de cursos seed al renombrar a SANTICAZA
            foreach (var seed in CourseCatalog.SeedCourses())
            {
                var existing = db.Courses.FirstOrDefault(c => c.Id == seed.Id || c.Slug == seed.Slug);
                if (existing == null) continue;
                existing.CertificateName = seed.CertificateName;
                existing.ThumbnailGradient = seed.ThumbnailGradient;
                existing.Instructor = seed.Instructor;
            }
            dirty = true;
        }

        // Regeneramos demos si faltan o si el hash no verifica (migración desde Node scrypt)
        var demo = db.Users.FirstOrDefault(u => u.Email is "demo@santicaza.com" or "demo@nexa.academy");
        var admin = db.Users.FirstOrDefault(u => u.Email is "admin@santicaza.com" or "admin@nexa.academy");
        var needsReseed =
            demo == null ||
            admin == null ||
            demo.Email != "demo@santicaza.com" ||
            admin.Email != "admin@santicaza.com" ||
            !PasswordService.Verify("demo1234", demo.PasswordHash) ||
            !PasswordService.Verify("admin1234", admin.PasswordHash);
        if (needsReseed)
        {
            var demos = await DemoUsersAsync();
            db.Users = db.Users
                .Where(u => u.Email is not (
                    "demo@nexa.academy" or "admin@nexa.academy" or
                    "demo@santicaza.com" or "admin@santicaza.com"))
                .ToList();
            db.Users.AddRange(demos);
            dirty = true;
        }

        if (dirty) await WriteUnlockedAsync(db);
        return db;
    }

    private async Task WriteUnlockedAsync(StoreData db)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
        var tmp = _dbPath + ".tmp";
        await using (var fs = File.Create(tmp))
        {
            await JsonSerializer.SerializeAsync(fs, db, JsonOpts);
        }
        File.Move(tmp, _dbPath, overwrite: true);
    }

    private async Task<T> MutateAsync<T>(Func<StoreData, T> mutator)
    {
        await _lock.WaitAsync();
        try
        {
            var db = await ReadAsync();
            var result = mutator(db);
            await WriteUnlockedAsync(db);
            return result;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task MutateAsync(Action<StoreData> mutator)
    {
        await MutateAsync<object?>(db =>
        {
            mutator(db);
            return null;
        });
    }

    private static async Task<StoreData> CreateSeedAsync() => new()
    {
        Users = await DemoUsersAsync(),
        Courses = CourseCatalog.SeedCourses(),
        Enrollments = new(),
        Orders = new(),
    };

    private static Task<List<UserAccount>> DemoUsersAsync()
    {
        var now = DateTime.UtcNow.ToString("o");
        return Task.FromResult(new List<UserAccount>
        {
            new()
            {
                Id = "user-demo",
                Name = "Estudiante Demo",
                Email = "demo@santicaza.com",
                PasswordHash = PasswordService.Hash("demo1234"),
                Role = "student",
                CreatedAt = now,
            },
            new()
            {
                Id = "user-admin",
                Name = "Admin SANTICAZA",
                Email = "admin@santicaza.com",
                PasswordHash = PasswordService.Hash("admin1234"),
                Role = "admin",
                CreatedAt = now,
            },
        });
    }

    public async Task<List<Course>> ListCoursesAsync(bool includeUnpublished = false)
    {
        await _lock.WaitAsync();
        try
        {
            var db = await ReadAsync();
            return includeUnpublished
                ? db.Courses.ToList()
                : db.Courses.Where(c => c.Published).ToList();
        }
        finally { _lock.Release(); }
    }

    public async Task<Course?> GetCourseBySlugAsync(string slug)
    {
        await _lock.WaitAsync();
        try
        {
            var db = await ReadAsync();
            return db.Courses.FirstOrDefault(c => c.Slug == slug);
        }
        finally { _lock.Release(); }
    }

    public async Task<Course?> GetCourseByIdAsync(string id)
    {
        await _lock.WaitAsync();
        try
        {
            var db = await ReadAsync();
            return db.Courses.FirstOrDefault(c => c.Id == id);
        }
        finally { _lock.Release(); }
    }

    public async Task<UserAccount?> FindUserByEmailAsync(string email)
    {
        await _lock.WaitAsync();
        try
        {
            var db = await ReadAsync();
            return db.Users.FirstOrDefault(u =>
                string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
        }
        finally { _lock.Release(); }
    }

    public async Task<UserAccount?> FindUserByIdAsync(string id)
    {
        await _lock.WaitAsync();
        try
        {
            var db = await ReadAsync();
            return db.Users.FirstOrDefault(u => u.Id == id);
        }
        finally { _lock.Release(); }
    }

    public async Task<List<UserAccount>> ListUsersAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var db = await ReadAsync();
            return db.Users.ToList();
        }
        finally { _lock.Release(); }
    }

    public Task<UserAccount> CreateUserAsync(string name, string email, string password, string role = "student")
    {
        return MutateAsync(db =>
        {
            if (db.Users.Any(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("EMAIL_TAKEN");

            var user = new UserAccount
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Email = email.ToLowerInvariant(),
                PasswordHash = PasswordService.Hash(password),
                Role = role,
                CreatedAt = DateTime.UtcNow.ToString("o"),
            };
            db.Users.Add(user);
            return user;
        });
    }

    public async Task<Enrollment?> GetEnrollmentAsync(string userId, string courseId)
    {
        await _lock.WaitAsync();
        try
        {
            var db = await ReadAsync();
            return db.Enrollments.FirstOrDefault(e => e.UserId == userId && e.CourseId == courseId);
        }
        finally { _lock.Release(); }
    }

    public async Task<List<Enrollment>> ListEnrollmentsForUserAsync(string userId)
    {
        await _lock.WaitAsync();
        try
        {
            var db = await ReadAsync();
            return db.Enrollments.Where(e => e.UserId == userId).ToList();
        }
        finally { _lock.Release(); }
    }

    public async Task<List<Order>> ListOrdersAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var db = await ReadAsync();
            return db.Orders.OrderByDescending(o => o.CreatedAt).ToList();
        }
        finally { _lock.Release(); }
    }

    public async Task<Order?> GetOrderByIdAsync(string id)
    {
        await _lock.WaitAsync();
        try
        {
            var db = await ReadAsync();
            return db.Orders.FirstOrDefault(o => o.Id == id);
        }
        finally { _lock.Release(); }
    }

    public Task<Order> CreatePendingOrderAsync(string userId, string courseId, decimal amount, string currency)
    {
        return MutateAsync(db =>
        {
            if (db.Orders.Any(o => o.UserId == userId && o.CourseId == courseId && o.Status == "paid"))
                throw new InvalidOperationException("ALREADY_OWNED");

            var order = new Order
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                CourseId = courseId,
                Amount = amount,
                Currency = currency,
                Status = "pending",
                CreatedAt = DateTime.UtcNow.ToString("o"),
                UpdatedAt = DateTime.UtcNow.ToString("o"),
            };
            db.Orders.Add(order);
            return order;
        });
    }

    public Task<Order> UpdateOrderAsync(string orderId, Action<Order> patch)
    {
        return MutateAsync(db =>
        {
            var order = db.Orders.FirstOrDefault(o => o.Id == orderId)
                ?? throw new InvalidOperationException("ORDER_NOT_FOUND");
            patch(order);
            order.UpdatedAt = DateTime.UtcNow.ToString("o");
            return order;
        });
    }

    public Task<(Order Order, Enrollment Enrollment)> FulfillPaidOrderAsync(string orderId)
    {
        return MutateAsync(db =>
        {
            var order = db.Orders.FirstOrDefault(o => o.Id == orderId)
                ?? throw new InvalidOperationException("ORDER_NOT_FOUND");
            order.Status = "paid";
            order.UpdatedAt = DateTime.UtcNow.ToString("o");

            var enrollment = db.Enrollments.FirstOrDefault(e =>
                e.UserId == order.UserId && e.CourseId == order.CourseId);
            if (enrollment == null)
            {
                enrollment = new Enrollment
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = order.UserId,
                    CourseId = order.CourseId,
                    PurchasedAt = DateTime.UtcNow.ToString("o"),
                    Progress = new(),
                    OrderId = order.Id,
                };
                db.Enrollments.Add(enrollment);
            }
            else
            {
                enrollment.OrderId = order.Id;
            }

            return (order, enrollment);
        });
    }

    public Task<Enrollment> MarkLessonCompleteAsync(string userId, string courseId, string lessonId)
    {
        return MutateAsync(db =>
        {
            var enrollment = db.Enrollments.FirstOrDefault(e => e.UserId == userId && e.CourseId == courseId)
                ?? throw new InvalidOperationException("NOT_ENROLLED");
            enrollment.Progress[lessonId] = true;

            var course = db.Courses.FirstOrDefault(c => c.Id == courseId);
            if (course?.IncludesCertificate == true && string.IsNullOrEmpty(enrollment.CertificateIssuedAt))
            {
                var all = course.Modules.SelectMany(m => m.Lessons).ToList();
                if (all.Count > 0 && all.All(l => enrollment.Progress.TryGetValue(l.Id, out var done) && done))
                {
                    enrollment.CertificateIssuedAt = DateTime.UtcNow.ToString("o");
                    var prefix = course.Slug.Length >= 4 ? course.Slug[..4].ToUpperInvariant() : course.Slug.ToUpperInvariant();
                    enrollment.CertificateCode = $"SCZ-{prefix}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
                }
            }

            return enrollment;
        });
    }

    public Task<Course> UpsertCourseAsync(Course input)
    {
        return MutateAsync(db =>
        {
            var now = DateTime.UtcNow.ToString("o");
            if (!string.IsNullOrEmpty(input.Id))
            {
                var idx = db.Courses.FindIndex(c => c.Id == input.Id);
                if (idx < 0) throw new InvalidOperationException("COURSE_NOT_FOUND");
                if (db.Courses.Any(c => c.Slug == input.Slug && c.Id != input.Id))
                    throw new InvalidOperationException("SLUG_TAKEN");
                input.UpdatedAt = now;
                db.Courses[idx] = input;
                return input;
            }

            if (db.Courses.Any(c => c.Slug == input.Slug))
                throw new InvalidOperationException("SLUG_TAKEN");

            input.Id = Guid.NewGuid().ToString();
            input.UpdatedAt = now;
            if (string.IsNullOrEmpty(input.CertificateName))
                input.CertificateName = $"Certificación SANTICAZA en {input.Title}";
            db.Courses.Add(input);
            return input;
        });
    }

    public Task DeleteCourseAsync(string id)
    {
        return MutateAsync(db =>
        {
            var before = db.Courses.Count;
            db.Courses = db.Courses.Where(c => c.Id != id).ToList();
            if (db.Courses.Count == before) throw new InvalidOperationException("COURSE_NOT_FOUND");
        });
    }

    public async Task<StoreStats> StatsAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var db = await ReadAsync();
            var paid = db.Orders.Where(o => o.Status == "paid").ToList();
            return new StoreStats(
                db.Users.Count,
                db.Courses.Count,
                db.Enrollments.Count,
                db.Orders.Count,
                paid.Sum(o => o.Amount));
        }
        finally { _lock.Release(); }
    }
}

public record StoreStats(int Users, int Courses, int Enrollments, int Orders, decimal Revenue);
