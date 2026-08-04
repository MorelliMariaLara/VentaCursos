using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexa.Web.Models;
using Nexa.Web.Services;

namespace Nexa.Web.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private readonly StoreService _store;

    public CheckoutController(StoreService store) => _store = store;

    [HttpGet]
    public async Task<IActionResult> Index(string slug, string? status)
    {
        var course = await _store.GetCourseBySlugAsync(slug);
        if (course == null) return NotFound();

        var enrollment = await _store.GetEnrollmentAsync(AuthCookie.UserId(User)!, course.Id);
        if (enrollment != null) return RedirectToAction("Index", "Learn", new { slug });

        return View(new CheckoutViewModel
        {
            Course = CourseMapper.ToPublic(course),
            PriceLabel = MoneyFormat.Ars(course.Price),
            Status = status,
        });
    }
}
