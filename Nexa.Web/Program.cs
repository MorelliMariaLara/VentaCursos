using Microsoft.AspNetCore.Authentication.Cookies;
using Nexa.Web.Services;

var builder = WebApplication.CreateBuilder(args);

LoadEnvFiles(builder);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<StoreService>();
builder.Services.AddSingleton<PaymentService>();
builder.Services.AddSingleton<StreamService>();
builder.Services.AddDataProtection();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "nexa_session";
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(72);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

var port = builder.Configuration["PORT"] ?? Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Urls.Clear();
app.Urls.Add($"http://0.0.0.0:{port}");

Console.WriteLine();
Console.WriteLine($"  NEXA MVC listo → http://localhost:{port}");
Console.WriteLine("  Alumno: demo@nexa.academy / demo1234");
Console.WriteLine("  Admin:  admin@nexa.academy / admin1234");
Console.WriteLine();

app.Run();

static void LoadEnvFiles(WebApplicationBuilder builder)
{
    foreach (var file in new[] { ".env.local", ".env" })
    {
        var full = Path.Combine(builder.Environment.ContentRootPath, "..", file);
        full = Path.GetFullPath(full);
        if (!File.Exists(full))
        {
            full = Path.Combine(builder.Environment.ContentRootPath, file);
            if (!File.Exists(full)) continue;
        }

        foreach (var line in File.ReadAllLines(full))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#')) continue;
            var i = trimmed.IndexOf('=');
            if (i <= 0) continue;
            var key = trimmed[..i].Trim();
            var val = trimmed[(i + 1)..].Trim();
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                Environment.SetEnvironmentVariable(key, val);
            builder.Configuration[key] = Environment.GetEnvironmentVariable(key) ?? val;
        }
    }

    // Prefer env vars already present
    foreach (var key in new[]
             {
                 "AUTH_SECRET", "STREAM_SECRET", "APP_URL", "PORT",
                 "MP_PUBLIC_KEY", "MP_ACCESS_TOKEN", "MP_ALLOW_SIMULATE", "MP_WEBHOOK_URL",
             })
    {
        var val = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrEmpty(val)) builder.Configuration[key] = val;
    }
}
