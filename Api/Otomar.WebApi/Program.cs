using Carter;
using Otomar.Application.Extensions;
using Otomar.Persistance.Extensions;
using Otomar.WebApi.Extensions;
using Otomar.WebApi.Middlewares;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    // kasA_ADI => KASA_ADI olarak gelsin diye eklendi
    options.SerializerOptions.PropertyNamingPolicy = null;
});
builder.Services.AddOptionsExtensions();

builder.Services.AddApplicationServices()
                .AddPersistanceServices()
                .AddWebApiServices(builder.Configuration, builder.Host);
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
app.MapCarter();
app.Run();