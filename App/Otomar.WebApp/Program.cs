using Microsoft.AspNetCore.Mvc;
using Otomar.WebApp.Extensions;

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
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Name = ".OTOMAR.WebAppSession";
    options.Cookie.MaxAge = TimeSpan.FromHours(24);
});
builder.Services.AddHttpContextAccessor();

builder.Services.AddOptionsExtensions();

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
app.UseHttpsRedirection();
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
app.UseRouting();
app.UseSession();
app.MapStaticAssets();
app.MapGet("/", () => Results.Redirect("/ana-sayfa"));

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}");

app.Run();