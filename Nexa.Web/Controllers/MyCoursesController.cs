using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexa.Web.Models;
using Nexa.Web.Services;

namespace Nexa.Web.Controllers;

[Authorize]
public class MyCoursesController : Controller
{
    private readonly StoreService _store;

    public MyCoursesController(StoreService store) => _store = store;

    public async Task<IActionResult> Index()
    {
        var userId = AuthCookie.UserId(User)!;
        var enrollments = await _store.ListEnrollmentsForUserAsync(userId);
        var courses = await _store.ListCoursesAsync(includeUnpublished: true);
        var items = enrollments
            .Select(e =>
            {
                var course = courses.FirstOrDefault(c => c.Id == e.CourseId);
                return course == null
                    ? null
                    : new MyCourseItemViewModel { Course = CourseMapper.ToPublic(course), Enrollment = e };
            })
            .Where(x => x != null)
            .Cast<MyCourseItemViewModel>()
            .ToList();
        return View(items);
    }
}
