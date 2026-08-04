using Microsoft.AspNetCore.Mvc;
using Nexa.Web.Models;
using Nexa.Web.Services;

namespace Nexa.Web.Controllers;

public class AccountController : Controller
{
    private readonly StoreService _store;

    public AccountController(StoreService store) => _store = store;

    [HttpGet]
    public IActionResult Login() => View(new LoginViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _store.FindUserByEmailAsync(model.Email.Trim());
        if (user == null || !PasswordService.Verify(model.Password, user.PasswordHash))
        {
            model.Error = "Credenciales inválidas";
            return View(model);
        }

        await AuthCookie.SignInAsync(HttpContext, user);
        return RedirectToAction("Index", "MyCourses");
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        try
        {
            var user = await _store.CreateUserAsync(model.Name.Trim(), model.Email.Trim(), model.Password);
            await AuthCookie.SignInAsync(HttpContext, user);
            return RedirectToAction("Index", "Courses");
        }
        catch (InvalidOperationException ex) when (ex.Message == "EMAIL_TAKEN")
        {
            model.Error = "Email ya registrado";
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await AuthCookie.SignOutAsync(HttpContext);
        return RedirectToAction("Index", "Home");
    }
}
