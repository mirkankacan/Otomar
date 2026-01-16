using Carter;
using Otomar.Application.Extensions;
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
}
app.MapHealthCheckServices();

app.MapCarter();
app.Run();