using LocalizationManager.Web.DataLayer;
using LocalizationManager.Web.DataLayer.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connection = builder.Configuration.GetConnectionString("LocalizationDb");
builder.Services.AddDbContext<LocalizationDbContext>(opt =>
    opt.UseNpgsql(connection));

builder.Services.AddMemoryCache();
builder.Services.AddControllersWithViews();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LocalizationDbContext>();
    db.Database.Migrate();

    if (!db.Cultures.Any())
    {
        db.Cultures.AddRange(
            new CultureEntity { Code = "en-US", DisplayName = "English" },
            new CultureEntity { Code = "uk-UA", DisplayName = "Українська" },
            new CultureEntity { Code = "ru-RU", DisplayName = "Русский" }
        );
        db.SaveChanges();
    }
}

app.UseStaticFiles();
app.MapControllers(); 
app.MapGet("/", () => "Localization Manager is running");

app.Run();