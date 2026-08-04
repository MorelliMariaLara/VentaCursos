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
    public async Task<IActionResult> Index(string slug, string? status, string? orderId)
    {
        var course = await _store.GetCourseBySlugAsync(slug);
        if (course == null) return NotFound();

        // Admin no paga: va directo al aula
        if (AuthCookie.IsAdmin(User))
        {
            await _store.EnsureCourseAccessAsync(AuthCookie.UserId(User)!, course.Id, isAdmin: true);
            return RedirectToAction("Index", "Learn", new { slug });
        }

        var enrollment = await _store.GetEnrollmentAsync(AuthCookie.UserId(User)!, course.Id);
        if (enrollment != null) return RedirectToAction("Index", "Learn", new { slug });

        // Si vuelve de MP con pago ya acreditado vía webhook, reintentar fulfill
        if (!string.IsNullOrEmpty(orderId) && status is "success" or "approved" or "paid")
        {
            var order = await _store.GetOrderByIdAsync(orderId);
            if (order != null && order.UserId == AuthCookie.UserId(User) && order.Status == "paid")
            {
                await _store.FulfillPaidOrderAsync(order.Id);
                return RedirectToAction("Index", "Learn", new { slug });
            }
        }

        return View(new CheckoutViewModel
        {
            Course = CourseMapper.ToPublic(course),
            PriceLabel = MoneyFormat.Ars(course.Price),
            Status = status,
        });
    }
}
