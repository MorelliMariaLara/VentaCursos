using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexa.Web.Models;
using Nexa.Web.Services;

namespace Nexa.Web.Controllers.Api;

[ApiController]
[Route("api")]
public class CoursesApiController : ControllerBase
{
    private readonly StoreService _store;

    public CoursesApiController(StoreService store) => _store = store;

    [HttpGet("courses")]
    public async Task<IActionResult> List()
    {
        var courses = await _store.ListCoursesAsync();
        return Ok(new { courses = courses.Select(CourseMapper.ToPublic) });
    }

    [HttpGet("courses/{slug}")]
    public async Task<IActionResult> Get(string slug)
    {
        var course = await _store.GetCourseBySlugAsync(slug);
        if (course == null || !course.Published) return NotFound(new { error = "No encontrado" });
        Enrollment? enrollment = null;
        var userId = AuthCookie.UserId(User);
        if (userId != null)
        {
            if (AuthCookie.IsAdmin(User))
                enrollment = await _store.EnsureCourseAccessAsync(userId, course.Id, isAdmin: true);
            else
                enrollment = await _store.GetEnrollmentAsync(userId, course.Id);
        }
        return Ok(new
        {
            course = CourseMapper.ToPublic(course),
            enrolled = enrollment != null,
            enrollment,
            priceLabel = MoneyFormat.Ars(course.Price),
            adminFreeAccess = AuthCookie.IsAdmin(User),
        });
    }

    [Authorize]
    [HttpGet("my-courses")]
    public async Task<IActionResult> MyCourses()
    {
        var userId = AuthCookie.UserId(User)!;
        var enrollments = await _store.ListEnrollmentsForUserAsync(userId);
        var courses = await _store.ListCoursesAsync(true);
        var items = enrollments
            .Select(e =>
            {
                var course = courses.FirstOrDefault(c => c.Id == e.CourseId);
                return course == null ? null : new { course = CourseMapper.ToPublic(course), enrollment = e };
            })
            .Where(x => x != null);
        return Ok(new { items });
    }

    [Authorize]
    [HttpGet("certificate/{slug}")]
    public async Task<IActionResult> Certificate(string slug)
    {
        var course = await _store.GetCourseBySlugAsync(slug);
        if (course == null) return NotFound(new { error = "No encontrado" });
        var enrollment = await _store.GetEnrollmentAsync(AuthCookie.UserId(User)!, course.Id);
        if (string.IsNullOrEmpty(enrollment?.CertificateCode))
            return NotFound(new { error = "Sin certificado" });
        return Ok(new
        {
            course = CourseMapper.ToPublic(course),
            certificateCode = enrollment.CertificateCode,
            issuedAt = enrollment.CertificateIssuedAt,
            studentName = AuthCookie.DisplayName(User),
        });
    }

    [Authorize]
    [HttpPost("progress")]
    public async Task<IActionResult> Progress([FromBody] ProgressBody body)
    {
        var course = await _store.GetCourseBySlugAsync(body.Slug ?? "");
        if (course == null) return NotFound(new { error = "Curso no encontrado" });
        try
        {
            await _store.EnsureCourseAccessAsync(
                AuthCookie.UserId(User)!, course.Id, AuthCookie.IsAdmin(User));
            var enrollment = await _store.MarkLessonCompleteAsync(
                AuthCookie.UserId(User)!, course.Id, body.LessonId ?? "");
            return Ok(new { enrollment });
        }
        catch (InvalidOperationException ex) when (ex.Message == "NOT_ENROLLED")
        {
            return StatusCode(403, new { error = "Sin acceso" });
        }
        catch (InvalidOperationException ex) when (ex.Message is "QUIZ_REQUIRED" or "VIDEO_REQUIRED")
        {
            return BadRequest(new
            {
                error = ex.Message == "VIDEO_REQUIRED"
                    ? "Tenés que ver el video completo."
                    : "Tenés que aprobar el cuestionario de la lección (60%).",
            });
        }
    }

    public record ProgressBody(string? Slug, string? LessonId);
}
