using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexa.Web.Models;
using Nexa.Web.Services;

namespace Nexa.Web.Controllers;

[Authorize(Roles = "admin")]
public class AdminController : Controller
{
    private readonly StoreService _store;

    public AdminController(StoreService store) => _store = store;

    public async Task<IActionResult> Index() => View(await BuildDashboardAsync());

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
            await _store.UpsertCourseAsync(new Course
            {
                Title = form.Title.Trim(),
                Slug = form.Slug.Trim(),
                Price = form.Price,
                Description = form.Description ?? "",
                Modules = new(),
                Published = true,
            });
            return RedirectToAction(nameof(Index));
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
