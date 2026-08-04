using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Nexa.Web.Data;
using Nexa.Web.Services;

var builder = WebApplication.CreateBuilder(args);

LoadEnvFiles(builder);
RepairBrokenMercadoPagoCredentials(builder);
// Evitar que Visual Studio / launchSettings dejen el simulador prendido
var mpPk = builder.Configuration["MP_PUBLIC_KEY"] ?? "";
var mpTk = builder.Configuration["MP_ACCESS_TOKEN"] ?? "";
if ((mpPk.StartsWith("TEST-", StringComparison.OrdinalIgnoreCase) ||
     mpPk.StartsWith("APP_USR-", StringComparison.OrdinalIgnoreCase)) &&
    (mpTk.StartsWith("TEST-", StringComparison.OrdinalIgnoreCase) ||
     mpTk.StartsWith("APP_USR-", StringComparison.OrdinalIgnoreCase)) &&
    !mpPk.Contains("TEST-APP_USR", StringComparison.OrdinalIgnoreCase) &&
    !mpTk.Contains("TEST-APP_USR", StringComparison.OrdinalIgnoreCase))
{
    Environment.SetEnvironmentVariable("MP_ALLOW_SIMULATE", "false");
    builder.Configuration["MP_ALLOW_SIMULATE"] = "false";
}

var connectionString =
    builder.Configuration.GetConnectionString("CursoVentas")
    ?? builder.Configuration["ConnectionStrings:CursoVentas"]
    ?? Environment.GetEnvironmentVariable("CONNECTION_STRING")
    ?? @"Server=LARA-NB\SQLEXPRESS02;Database=CursoVentas;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddScoped<StoreService>();
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

using (var scope = app.Services.CreateScope())
{
    var store = scope.ServiceProvider.GetRequiredService<StoreService>();
    try
    {
        await store.EnsureSeedAsync();
        Console.WriteLine("  SQL Server: seed verificado (CursoVentas)");
    }
    catch (Exception ex)
    {
        Console.WriteLine("  AVISO: no se pudo conectar/sembrar SQL Server.");
        Console.WriteLine("  " + ex.Message);
        Console.WriteLine("  Ejecutá database/01_CreateTables.sql en LARA-NB\\SQLEXPRESS02 / CursoVentas");
    }
}

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
Console.WriteLine($"  SANTICAZA Capacitaciones → http://localhost:{port}");
Console.WriteLine(@"  SQL: LARA-NB\SQLEXPRESS02 · CursoVentas");
try
{
    using var scopeDiag = app.Services.CreateScope();
    var pay = scopeDiag.ServiceProvider.GetRequiredService<PaymentService>();
    Console.WriteLine("  " + pay.CredentialDiagnostics());
}
catch { /* ignore */ }
Console.WriteLine("  Alumno: demo@santicaza.com / demo1234");
Console.WriteLine("  Admin:  admin@santicaza.com / admin1234");
Console.WriteLine();

app.Run();

/// <summary>
/// Si el usuario editó mal el .env (TEST-APP_USR-… o le borró APP_USR dejando "-032d…"),
/// restauramos el par TEST conocido del proyecto y reescribimos .env.
/// </summary>
static void RepairBrokenMercadoPagoCredentials(WebApplicationBuilder builder)
{
    const string goodPk = "TEST-de2c8c3d-972c-4a5b-a05c-22745894b73a";
    const string goodTk = "TEST-2564533232408086-080413-6bb40d3c790d8550063469c4e6000620-706865166";

    var pk = (builder.Configuration["MP_PUBLIC_KEY"] ?? "").Trim();
    var tk = (builder.Configuration["MP_ACCESS_TOKEN"] ?? "").Trim();

    static bool Ok(string v) =>
        (v.StartsWith("TEST-", StringComparison.OrdinalIgnoreCase) ||
         v.StartsWith("APP_USR-", StringComparison.OrdinalIgnoreCase)) &&
        !v.Contains("TEST-APP_USR", StringComparison.OrdinalIgnoreCase);

    if (Ok(pk) && Ok(tk) &&
        ((pk.StartsWith("TEST-", StringComparison.OrdinalIgnoreCase) &&
          tk.StartsWith("TEST-", StringComparison.OrdinalIgnoreCase)) ||
         (pk.StartsWith("APP_USR-", StringComparison.OrdinalIgnoreCase) &&
          tk.StartsWith("APP_USR-", StringComparison.OrdinalIgnoreCase))))
    {
        return;
    }

    Console.WriteLine("  AVISO: MP_PUBLIC_KEY / MP_ACCESS_TOKEN inválidos o mezclados.");
    Console.WriteLine("  Restaurando par de Pruebas del proyecto (TEST-de2c… / TEST-2564…).");

    Environment.SetEnvironmentVariable("MP_PUBLIC_KEY", goodPk);
    Environment.SetEnvironmentVariable("MP_ACCESS_TOKEN", goodTk);
    builder.Configuration["MP_PUBLIC_KEY"] = goodPk;
    builder.Configuration["MP_ACCESS_TOKEN"] = goodTk;

    foreach (var envPath in new[]
             {
                 Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", ".env")),
                 Path.Combine(builder.Environment.ContentRootPath, ".env"),
             })
    {
        try
        {
            if (!File.Exists(envPath)) continue;
            var lines = File.ReadAllLines(envPath).Select(line =>
            {
                if (line.StartsWith("MP_PUBLIC_KEY=", StringComparison.OrdinalIgnoreCase))
                    return "MP_PUBLIC_KEY=" + goodPk;
                if (line.StartsWith("MP_ACCESS_TOKEN=", StringComparison.OrdinalIgnoreCase))
                    return "MP_ACCESS_TOKEN=" + goodTk;
                return line;
            }).ToList();
            File.WriteAllLines(envPath, lines);
            Console.WriteLine($"  .env reparado: {envPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("  No se pudo reescribir .env: " + ex.Message);
        }
    }
}

static void LoadEnvFiles(WebApplicationBuilder builder)
{
    // Orden: .env primero, .env.local después (gana local).
    // Siempre pisan variables viejas del sistema: si no, Windows/IDE puede
    // dejar MP_ACCESS_TOKEN=APP_USR-... y el .env con TEST- se ignora → UNAUTHORIZED.
    foreach (var file in new[] { ".env", ".env.local" })
    {
        foreach (var candidate in new[]
                 {
                     Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", file)),
                     Path.Combine(builder.Environment.ContentRootPath, file),
                 })
        {
            if (!File.Exists(candidate)) continue;
            Console.WriteLine($"  .env cargado: {candidate}");
            foreach (var line in File.ReadAllLines(candidate))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#')) continue;
                var i = trimmed.IndexOf('=');
                if (i <= 0) continue;
                var key = trimmed[..i].Trim();
                var val = trimmed[(i + 1)..].Trim().Trim('"').Trim('\'');
                Environment.SetEnvironmentVariable(key, val);
                builder.Configuration[key] = val;
                if (key == "CONNECTION_STRING")
                    builder.Configuration["ConnectionStrings:CursoVentas"] = val;
            }
            break; // un path por nombre de archivo
        }
    }

    foreach (var key in new[]
             {
                 "AUTH_SECRET", "STREAM_SECRET", "APP_URL", "PORT",
                 "MP_PUBLIC_KEY", "MP_ACCESS_TOKEN", "MP_ALLOW_SIMULATE", "MP_WEBHOOK_URL",
                 "CONNECTION_STRING",
             })
    {
        var val = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrEmpty(val))
        {
            builder.Configuration[key] = val;
            if (key == "CONNECTION_STRING")
                builder.Configuration["ConnectionStrings:CursoVentas"] = val;
        }
    }
}
