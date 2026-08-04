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
        var isAdmin = AuthCookie.IsAdmin(User);
        var courses = await _store.ListCoursesAsync(includeUnpublished: isAdmin);

        // Admin: acceso a todos los cursos sin pagar
        if (isAdmin)
        {
            var items = new List<MyCourseItemViewModel>();
            foreach (var course in courses)
            {
                var enrollment = await _store.EnsureCourseAccessAsync(userId, course.Id, isAdmin: true);
                items.Add(new MyCourseItemViewModel
                {
                    Course = CourseMapper.ToPublic(course),
                    Enrollment = enrollment,
                });
            }
            return View(items);
        }

        var enrollments = await _store.ListEnrollmentsForUserAsync(userId);
        var studentItems = enrollments
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
        return View(studentItems);
    }
}
