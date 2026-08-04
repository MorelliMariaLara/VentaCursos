using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexa.Web.Models;
using Nexa.Web.Services;

namespace Nexa.Web.Controllers.Api;

[ApiController]
[Route("api/stream")]
public class StreamApiController : ControllerBase
{
    private readonly StoreService _store;
    private readonly StreamService _stream;

    public StreamApiController(StoreService store, StreamService stream)
    {
        _store = store;
        _stream = stream;
    }

    public record SessionBody(string? Slug, string? LessonId);

    [Authorize]
    [HttpPost("session")]
    public async Task<IActionResult> Session([FromBody] SessionBody body)
    {
        var userId = AuthCookie.UserId(User)!;
        var course = await _store.GetCourseBySlugAsync(body.Slug ?? "");
        if (course == null) return NotFound(new { error = "Curso no encontrado" });
        if (await _store.GetEnrollmentAsync(userId, course.Id) == null)
            return StatusCode(403, new { error = "Sin acceso" });

        var found = CourseMapper.FindLesson(course, body.LessonId ?? "");
        if (found == null) return NotFound(new { error = "Lección no encontrada" });

        var (key, iv) = _stream.GenerateContentKey();
        var token = _stream.CreateStreamToken(userId, course.Id, found.Value.Lesson.Id, key, iv);
        return Ok(new
        {
            token,
            keyB64 = Convert.ToBase64String(key),
            ivB64 = Convert.ToBase64String(iv),
            watermark = StreamService.WatermarkFingerprint(userId, AuthCookie.Email(User)!),
            mediaUrl = $"/api/stream/media?token={Uri.EscapeDataString(token)}",
            lesson = new
            {
                id = found.Value.Lesson.Id,
                title = found.Value.Lesson.Title,
                moduleTitle = found.Value.ModuleTitle,
            },
        });
    }

    [HttpGet("media")]
    public async Task<IActionResult> Media([FromQuery] string token)
    {
        var claims = _stream.VerifyStreamToken(token ?? "");
        if (claims == null) return Unauthorized("unauthorized");
        var course = await _store.GetCourseByIdAsync(claims.CourseId);
        if (course == null) return NotFound("missing");
        var found = CourseMapper.FindLesson(course, claims.LessonId);
        if (found == null) return NotFound("missing");

        try
        {
            var src = await _stream.ReadVideoSourceAsync(found.Value.Lesson.SourceUrl, Request.Headers.Range);
            var key = Convert.FromBase64String(claims.KeyB64);
            var iv = Convert.FromBase64String(claims.IvB64);
            var encrypted = StreamService.EncryptChunk(src.Body, key, iv, src.Start);
            Response.StatusCode = src.Status;
            Response.Headers.CacheControl = "no-store";
            Response.Headers["X-Content-Encoding"] = "aes-256-ctr";
            Response.ContentType = "application/octet-stream";
            Response.ContentLength = encrypted.Length;
            if (src.ContentRange != null)
                Response.Headers.ContentRange = src.ContentRange;
            await Response.Body.WriteAsync(encrypted);
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return StatusCode(500, "error");
        }
    }
}
