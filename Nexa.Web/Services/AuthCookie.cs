using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Nexa.Web.Models;

namespace Nexa.Web.Services;

public static class AuthCookie
{
    public const string Scheme = CookieAuthenticationDefaults.AuthenticationScheme;

    public static ClaimsPrincipal CreatePrincipal(UserAccount user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme));
    }

    public static async Task SignInAsync(HttpContext http, UserAccount user)
    {
        await http.SignInAsync(
            Scheme,
            CreatePrincipal(user),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(72),
            });
    }

    public static Task SignOutAsync(HttpContext http) => http.SignOutAsync(Scheme);

    public static string? UserId(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier);

    public static string? Email(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Email);

    public static string? DisplayName(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Name);

    public static bool IsAdmin(ClaimsPrincipal user) =>
        user.IsInRole("admin");
}

public static class MoneyFormat
{
    public static string Ars(decimal n) =>
        string.Create(System.Globalization.CultureInfo.GetCultureInfo("es-AR"), $"ARS {n:N0}");
}
