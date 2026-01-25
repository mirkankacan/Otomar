using Carter;
using Microsoft.AspNetCore.Identity;
using Otomar.Application.Extensions;
using Otomar.Domain.Entities;
using Otomar.Persistance.Data;
using Otomar.Persistance.Extensions;
using Otomar.WebApi.Extensions;
using Otomar.WebApi.Middlewares;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptionsExtensions();

builder.Services.AddApplicationServices()
                .AddPersistanceServices(builder.Configuration)
                .AddWebApiServices(builder.Configuration, builder.Host);

builder.Services.AddHealthCheckServices(builder.Configuration);

var app = builder.Build();
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Otomar.WebApi v1");
        options.RoutePrefix = "swagger";
    });

    // Seed data for development
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<IdentityDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

            await SeedData.SeedAsync(context, userManager, roleManager);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }
}

app.MapHealthCheckServices();

app.MapCarter();
app.Run();