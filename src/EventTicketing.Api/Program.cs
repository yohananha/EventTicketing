using EventTicketing.Api.BackgroundServices;
using EventTicketing.Api.Middleware;
using EventTicketing.BusinessLogic;
using EventTicketing.DataAccess;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Hold timings (duration / sweep interval) come from the "Hold" config section.
builder.Services.Configure<HoldSettings>(builder.Configuration.GetSection("Hold"));

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Missing connection string 'Default'.");

builder.Services
    .AddDataAccess(connectionString)
    .AddBusinessLogic();

builder.Services.AddHostedService<HoldExpiryBackgroundService>();

var app = builder.Build();

// --- Schema + seed ----------------------------------------------------------
// Apply migrations on startup, then seed sample data in Development.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    if (app.Environment.IsDevelopment())
        await DataSeeder.SeedAsync(db);
}

// --- Pipeline ---------------------------------------------------------------
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

// Exposed so integration tests can reference the entry assembly via WebApplicationFactory.
public partial class Program { }
