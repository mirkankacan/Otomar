using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Otomar.WebApp.Extensions;
using Otomar.WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "__RequestVerificationToken";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
}).AddRazorRuntimeCompilation();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Name = ".OTOMAR.WebAppSession";
    options.Cookie.MaxAge = TimeSpan.FromHours(24);
});
builder.Services.AddHttpContextAccessor();

builder.Services.AddOptionsExtensions();

builder.Services.AddScoped<IIdentityService, IdentityService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = ".OTOMAR.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;
        options.LoginPath = "/giris-yap";
        options.LogoutPath = "/cikis-yap";
        options.AccessDeniedPath = "/erisim-engellendi";
    });
builder.Services.AddAuthorization();

// Refit API Clients
builder.Services.AddRefitClients(builder.Configuration);

//builder.Services.AddHealthChecks()
//    .AddCheck<BackendApiHealthCheck>(
//        "backend-api",
//        failureStatus: HealthStatus.Unhealthy,
//        tags: new[] { "api" });
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/hata/{0}");
app.UseHttpsRedirection();

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    await next();
});

// www -> non-www 301 redirect
app.Use(async (context, next) =>
{
    var host = context.Request.Host.Host;
    if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
    {
        var newHost = host[4..]; // "www." kısmını kaldır
        var newUrl = $"{context.Request.Scheme}://{newHost}{context.Request.Path}{context.Request.QueryString}";
        context.Response.StatusCode = 301;
        context.Response.Headers.Location = newUrl;
        return;
    }
    await next();
});
//app.MapHealthChecks("/health", new HealthCheckOptions
//{
//    Predicate = _ => true,
//    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
//    ResultStatusCodes =
//    {
//        [HealthStatus.Healthy] = StatusCodes.Status200OK,
//        [HealthStatus.Degraded] = StatusCodes.Status200OK,
//        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
//    }
//});

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        const int durationInSeconds = 60 * 60 * 24 * 365;
        ctx.Context.Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.CacheControl] =
            "public,max-age=" + durationInSeconds;
    }
});
app.Use(async (context, next) =>
{
    if (context.Request.Path.Value == "/")
    {
        context.Response.Redirect("/ana-sayfa", true);
        return;
    }
    await next();
});
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}");

app.Run();