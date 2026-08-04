using Microsoft.AspNetCore.Mvc;
using Nexa.Web.Services;

namespace Nexa.Web.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly StoreService _store;

    public AuthApiController(StoreService store) => _store = store;

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var id = AuthCookie.UserId(User);
        if (id == null) return Ok(new { user = (object?)null });
        var user = await _store.FindUserByIdAsync(id);
        if (user == null) return Ok(new { user = (object?)null });
        return Ok(new { user = new { id = user.Id, name = user.Name, email = user.Email, role = user.Role } });
    }

    public record AuthBody(string? Email, string? Password, string? Name);

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthBody body)
    {
        var user = await _store.FindUserByEmailAsync((body.Email ?? "").Trim());
        if (user == null || !PasswordService.Verify(body.Password ?? "", user.PasswordHash))
            return Unauthorized(new { error = "Credenciales inválidas" });
        await AuthCookie.SignInAsync(HttpContext, user);
        return Ok(new { user = new { id = user.Id, name = user.Name, email = user.Email, role = user.Role } });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] AuthBody body)
    {
        var name = (body.Name ?? "").Trim();
        var email = (body.Email ?? "").Trim();
        var password = body.Password ?? "";
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || password.Length < 6)
            return BadRequest(new { error = "Datos incompletos" });
        try
        {
            var user = await _store.CreateUserAsync(name, email, password);
            await AuthCookie.SignInAsync(HttpContext, user);
            return Ok(new { user = new { id = user.Id, name = user.Name, email = user.Email, role = user.Role } });
        }
        catch (InvalidOperationException ex) when (ex.Message == "EMAIL_TAKEN")
        {
            return Conflict(new { error = "Email ya registrado" });
        }
        catch
        {
            return StatusCode(500, new { error = "No se pudo registrar" });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await AuthCookie.SignOutAsync(HttpContext);
        return Ok(new { ok = true });
    }
}
