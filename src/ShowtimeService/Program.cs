using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using ShowtimeService.Data;
using ShowtimeService.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ShowtimeDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ShowtimeDb")));

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Showtime Service", Version = "v1" });
    c.SwaggerDoc("v2", new OpenApiInfo { Title = "Showtime Service", Version = "v2" });
    c.OperationFilter<DeprecateV1OperationFilter>();
});

var app = builder.Build();

// Beim Build laedt GetDocument.Insider die App nur, um swagger.json zu erzeugen: dann keine DB anfassen.
var isOpenApiGeneration = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";

if (!isOpenApiGeneration)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ShowtimeDbContext>();
    db.Database.Migrate();
    SeedData.Seed(db);
    SeedData.RefreshShowtimeDates(db);
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v2/swagger.json", "v2");
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "v1 (veraltet)");
    });
}

app.MapApiEndpoints();
app.MapHealthChecks("/health/ready");

app.Run();
