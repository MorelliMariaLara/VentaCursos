using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexa.Web.Models;
using Nexa.Web.Services;

namespace Nexa.Web.Controllers;

[Authorize(Roles = "admin")]
public class AdminController : Controller
{
    private readonly StoreService _store;
    private readonly StreamService _stream;

    public AdminController(StoreService store, StreamService stream)
    {
        _store = store;
        _stream = stream;
    }

    public async Task<IActionResult> Index() => View(await BuildDashboardAsync());

    [HttpGet]
    public async Task<IActionResult> Course(string id, string? message = null, string? error = null)
    {
        var course = await _store.GetCourseByIdAsync(id);
        if (course == null) return NotFound();

        var questionsByLesson = new Dictionary<string, List<LessonQuestion>>();
        foreach (var les in course.Modules.SelectMany(m => m.Lessons))
            questionsByLesson[les.Id] = await _store.ListQuestionsForLessonAsync(les.Id, includeCorrect: true);

        return View(new AdminCourseEditViewModel
        {
            Course = course,
            QuestionsByLesson = questionsByLesson,
            Message = message,
            Error = error,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCourse(AdminCourseForm form)
    {
        if (!ModelState.IsValid)
        {
            var vm = await BuildDashboardAsync();
            vm.NewCourse = form;
            vm.Error = "Datos incompletos";
            return View("Index", vm);
        }

        try
        {
            var course = await _store.UpsertCourseAsync(new Course
            {
                Title = form.Title.Trim(),
                Slug = form.Slug.Trim(),
                Price = form.Price,
                Description = form.Description ?? "",
                Modules = new(),
                Published = true,
            });
            return RedirectToAction(nameof(Course), new { id = course.Id, message = "Curso creado. Ahora cargá módulos y lecciones." });
        }
        catch (Exception ex)
        {
            var vm = await BuildDashboardAsync();
            vm.NewCourse = form;
            vm.Error = ex.Message;
            return View("Index", vm);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCourse(string id)
    {
        try { await _store.DeleteCourseAsync(id); }
        catch { /* ignore */ }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddModule(AdminModuleForm form)
    {
        try
        {
            await _store.AddModuleAsync(form.CourseId, form.Title);
            return RedirectToAction(nameof(Course), new { id = form.CourseId, message = "Módulo agregado." });
        }
        catch (Exception ex)
        {
            return RedirectToAction(nameof(Course), new { id = form.CourseId, error = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteModule(string courseId, string moduleId)
    {
        try { await _store.DeleteModuleAsync(moduleId); }
        catch (Exception ex)
        {
            return RedirectToAction(nameof(Course), new { id = courseId, error = ex.Message });
        }
        return RedirectToAction(nameof(Course), new { id = courseId, message = "Módulo eliminado." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(520_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 520_000_000)]
    public async Task<IActionResult> AddLesson(AdminLessonForm form, IFormFile? videoFile)
    {
        try
        {
            string sourceUrl;
            if (string.Equals(form.SourceType, "upload", StringComparison.OrdinalIgnoreCase))
            {
                if (videoFile == null || videoFile.Length == 0)
                    throw new InvalidOperationException("Seleccioná un archivo de video.");
                sourceUrl = await _stream.SaveUploadedVideoAsync(videoFile);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(form.YoutubeUrl))
                    throw new InvalidOperationException("Pegá el link de YouTube (puede ser no listado / privado según tu cuenta).");
                sourceUrl = VideoSources.Normalize(form.YoutubeUrl);
            }

            await _store.AddLessonAsync(form.ModuleId, form.Title, form.DurationMinutes, sourceUrl);
            return RedirectToAction(nameof(Course), new { id = form.CourseId, message = "Lección guardada." });
        }
        catch (Exception ex)
        {
            return RedirectToAction(nameof(Course), new { id = form.CourseId, error = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(520_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 520_000_000)]
    public async Task<IActionResult> UpdateLesson(
        string courseId,
        string lessonId,
        string title,
        int durationMinutes,
        string sourceType,
        string? youtubeUrl,
        IFormFile? videoFile)
    {
        try
        {
            string? newSource = null;
            if (string.Equals(sourceType, "upload", StringComparison.OrdinalIgnoreCase) &&
                videoFile != null && videoFile.Length > 0)
            {
                newSource = await _stream.SaveUploadedVideoAsync(videoFile);
            }
            else if (string.Equals(sourceType, "youtube", StringComparison.OrdinalIgnoreCase) &&
                     !string.IsNullOrWhiteSpace(youtubeUrl))
            {
                newSource = VideoSources.Normalize(youtubeUrl);
            }

            await _store.UpdateLessonAsync(lessonId, title, durationMinutes, newSource);
            return RedirectToAction(nameof(Course), new { id = courseId, message = "Lección actualizada." });
        }
        catch (Exception ex)
        {
            return RedirectToAction(nameof(Course), new { id = courseId, error = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLesson(string courseId, string lessonId)
    {
        try { await _store.DeleteLessonAsync(lessonId); }
        catch (Exception ex)
        {
            return RedirectToAction(nameof(Course), new { id = courseId, error = ex.Message });
        }
        return RedirectToAction(nameof(Course), new { id = courseId, message = "Lección eliminada." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddQuestion(AdminQuestionForm form)
    {
        try
        {
            var options = new List<(string Text, bool IsCorrect)>();
            void Add(string? text, string key)
            {
                if (string.IsNullOrWhiteSpace(text)) return;
                options.Add((text.Trim(), string.Equals(form.CorrectOption, key, StringComparison.OrdinalIgnoreCase)));
            }
            Add(form.OptionA, "A");
            Add(form.OptionB, "B");
            Add(form.OptionC, "C");
            Add(form.OptionD, "D");

            await _store.AddQuestionAsync(form.LessonId, form.Prompt, options);
            return RedirectToAction(nameof(Course), new { id = form.CourseId, message = "Pregunta agregada." });
        }
        catch (Exception ex)
        {
            return RedirectToAction(nameof(Course), new { id = form.CourseId, error = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuestion(string courseId, string questionId)
    {
        try { await _store.DeleteQuestionAsync(questionId); }
        catch (Exception ex)
        {
            return RedirectToAction(nameof(Course), new { id = courseId, error = ex.Message });
        }
        return RedirectToAction(nameof(Course), new { id = courseId, message = "Pregunta eliminada." });
    }

    private async Task<AdminDashboardViewModel> BuildDashboardAsync()
    {
        var stats = await _store.StatsAsync();
        return new AdminDashboardViewModel
        {
            Users = stats.Users,
            Courses = stats.Courses,
            Enrollments = stats.Enrollments,
            Orders = stats.Orders,
            Revenue = stats.Revenue,
            RecentOrders = (await _store.ListOrdersAsync()).Take(20).ToList(),
            UserList = await _store.ListUsersAsync(),
            CourseList = (await _store.ListCoursesAsync(true)).Select(CourseMapper.ToPublic).ToList(),
        };
    }
}
