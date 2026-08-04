using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexa.Web.Models;
using Nexa.Web.Services;

namespace Nexa.Web.Controllers;

[Authorize]
public class LearnController : Controller
{
    private readonly StoreService _store;

    public LearnController(StoreService store) => _store = store;

    [HttpGet]
    public async Task<IActionResult> Index(string slug)
    {
        var course = await _store.GetCourseBySlugAsync(slug);
        if (course == null) return NotFound();

        try
        {
            var enrollment = await _store.EnsureCourseAccessAsync(
                AuthCookie.UserId(User)!, course.Id, AuthCookie.IsAdmin(User));
            return View(new LearnViewModel
            {
                Course = CourseMapper.ToPublic(course),
                Enrollment = enrollment,
            });
        }
        catch (InvalidOperationException ex) when (ex.Message == "NOT_ENROLLED")
        {
            return RedirectToAction("Index", "Checkout", new { slug });
        }
    }
}
