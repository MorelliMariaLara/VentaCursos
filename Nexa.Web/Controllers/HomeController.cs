using Microsoft.AspNetCore.Mvc;
using Nexa.Web.Models;
using Nexa.Web.Services;

namespace Nexa.Web.Controllers;

public class HomeController : Controller
{
    private readonly StoreService _store;

    public HomeController(StoreService store) => _store = store;

    public async Task<IActionResult> Index()
    {
        var courses = await _store.ListCoursesAsync();
        return View(courses.Take(3).Select(CourseMapper.ToPublic).ToList());
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
}
