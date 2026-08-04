using Microsoft.AspNetCore.Mvc;
using Nexa.Web.Models;
using Nexa.Web.Services;

namespace Nexa.Web.Controllers;

public class CoursesController : Controller
{
    private readonly StoreService _store;

    public CoursesController(StoreService store) => _store = store;

    public async Task<IActionResult> Index()
    {
        var courses = await _store.ListCoursesAsync();
        return View(courses.Select(CourseMapper.ToPublic).ToList());
    }

    [HttpGet("/Courses/{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        var course = await _store.GetCourseBySlugAsync(slug);
        if (course == null || !course.Published) return NotFound();

        Enrollment? enrollment = null;
        var userId = AuthCookie.UserId(User);
        var isAdmin = userId != null && AuthCookie.IsAdmin(User);
        if (userId != null)
        {
            enrollment = isAdmin
                ? await _store.EnsureCourseAccessAsync(userId, course.Id, isAdmin: true)
                : await _store.GetEnrollmentAsync(userId, course.Id);
        }

        return View(new CourseDetailViewModel
        {
            Course = CourseMapper.ToPublic(course),
            Enrolled = enrollment != null,
            Enrollment = enrollment,
            PriceLabel = MoneyFormat.Ars(course.Price),
            AdminFreeAccess = isAdmin,
        });
    }
}
