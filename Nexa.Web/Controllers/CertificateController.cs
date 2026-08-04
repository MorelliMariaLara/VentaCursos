using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexa.Web.Models;
using Nexa.Web.Services;

namespace Nexa.Web.Controllers;

[Authorize]
public class CertificateController : Controller
{
    private readonly StoreService _store;

    public CertificateController(StoreService store) => _store = store;

    [HttpGet]
    public async Task<IActionResult> Index(string slug)
    {
        var course = await _store.GetCourseBySlugAsync(slug);
        if (course == null) return NotFound();

        Enrollment enrollment;
        try
        {
            enrollment = await _store.EnsureCourseAccessAsync(
                AuthCookie.UserId(User)!, course.Id, AuthCookie.IsAdmin(User));
        }
        catch (InvalidOperationException ex) when (ex.Message == "NOT_ENROLLED")
        {
            return RedirectToAction("Index", "Checkout", new { slug });
        }

        if (string.IsNullOrEmpty(enrollment.CertificateCode))
            return RedirectToAction("Index", "Learn", new { slug });

        return View(new CertificateViewModel
        {
            Course = CourseMapper.ToPublic(course),
            CertificateCode = enrollment.CertificateCode!,
            IssuedAt = enrollment.CertificateIssuedAt!,
            StudentName = AuthCookie.DisplayName(User) ?? "",
        });
    }
}
