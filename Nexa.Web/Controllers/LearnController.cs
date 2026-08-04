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

        var enrollment = await _store.GetEnrollmentAsync(AuthCookie.UserId(User)!, course.Id);
        if (enrollment == null) return RedirectToAction("Index", "Checkout", new { slug });

        return View(new LearnViewModel
        {
            Course = CourseMapper.ToPublic(course),
            Enrollment = enrollment,
        });
    }
}
