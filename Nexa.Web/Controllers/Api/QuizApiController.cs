using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexa.Web.Models;
using Nexa.Web.Services;

namespace Nexa.Web.Controllers.Api;

[ApiController]
[Route("api")]
[Authorize]
public class QuizApiController : ControllerBase
{
    private readonly StoreService _store;

    public QuizApiController(StoreService store) => _store = store;

    public record WatchedBody(string? Slug, string? LessonId);
    public record QuizAnswerBody(string? Slug, string? LessonId, Dictionary<string, string>? Answers);

    [HttpPost("progress/watched")]
    public async Task<IActionResult> MarkWatched([FromBody] WatchedBody body)
    {
        var course = await _store.GetCourseBySlugAsync(body.Slug ?? "");
        if (course == null) return NotFound(new { error = "Curso no encontrado" });
        try
        {
            await _store.EnsureCourseAccessAsync(
                AuthCookie.UserId(User)!, course.Id, AuthCookie.IsAdmin(User));
            var enrollment = await _store.MarkVideoWatchedAsync(
                AuthCookie.UserId(User)!, course.Id, body.LessonId ?? "");
            var questions = await _store.ListQuestionsForLessonAsync(body.LessonId ?? "", includeCorrect: false);
            return Ok(new
            {
                enrollment,
                videoWatched = true,
                hasQuiz = questions.Count > 0,
                questionCount = questions.Count,
            });
        }
        catch (InvalidOperationException ex) when (ex.Message == "NOT_ENROLLED")
        {
            return StatusCode(403, new { error = "Sin acceso" });
        }
    }

    [HttpGet("lessons/{lessonId}/quiz")]
    public async Task<IActionResult> GetQuiz(string lessonId, [FromQuery] string? slug)
    {
        var course = await _store.GetCourseBySlugAsync(slug ?? "");
        if (course == null) return NotFound(new { error = "Curso no encontrado" });
        Enrollment enrollment;
        try
        {
            enrollment = await _store.EnsureCourseAccessAsync(
                AuthCookie.UserId(User)!, course.Id, AuthCookie.IsAdmin(User));
        }
        catch (InvalidOperationException ex) when (ex.Message == "NOT_ENROLLED")
        {
            return StatusCode(403, new { error = "Sin acceso" });
        }

        var watched = enrollment.VideoWatched.TryGetValue(lessonId, out var w) && w;
        if (!watched)
            return StatusCode(403, new { error = "Tenés que ver el video completo antes de responder.", videoRequired = true });

        var questions = await _store.ListQuestionsForLessonAsync(lessonId, includeCorrect: false);
        return Ok(new
        {
            lessonId,
            passPercent = StoreService.PassPercent,
            questions = questions.Select(q => new
            {
                q.Id,
                q.Prompt,
                answers = q.Answers.Select(a => new { a.Id, a.Text }),
            }),
        });
    }

    [HttpPost("lessons/quiz")]
    public async Task<IActionResult> SubmitQuiz([FromBody] QuizAnswerBody body)
    {
        var course = await _store.GetCourseBySlugAsync(body.Slug ?? "");
        if (course == null) return NotFound(new { error = "Curso no encontrado" });
        try
        {
            await _store.EnsureCourseAccessAsync(
                AuthCookie.UserId(User)!, course.Id, AuthCookie.IsAdmin(User));
            var result = await _store.SubmitLessonQuizAsync(
                AuthCookie.UserId(User)!,
                course.Id,
                body.LessonId ?? "",
                body.Answers ?? new Dictionary<string, string>());
            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message == "NOT_ENROLLED")
        {
            return StatusCode(403, new { error = "Sin acceso" });
        }
        catch (InvalidOperationException ex) when (ex.Message == "VIDEO_REQUIRED")
        {
            return StatusCode(403, new { error = "Tenés que ver el video completo antes de responder.", videoRequired = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
