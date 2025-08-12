using LocalizationManager.Web.DataLayer;
using LocalizationManager.Web.DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using LocalizationManager.Web.Utils;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

var connection = builder.Configuration.GetConnectionString("LocalizationDb");

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<LocalizationDbContext>(o =>
        o.UseInMemoryDatabase("loc-tests-db"));
}
else
{
    builder.Services.AddDbContext<LocalizationDbContext>(opt =>
        opt.UseNpgsql(connection));
}


builder.Services.AddMemoryCache();
builder.Services.AddControllersWithViews();
builder.Services.AddLocalization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IStringLocalizer, DbStringLocalizer>();
builder.Services.AddScoped<IAsyncStringLocalizer, DbStringLocalizer>();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var cultures = new List<CultureInfo> { new("en-US"), new("uk-UA"), new("ru-RU") };
    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = cultures;
    options.SupportedUICultures = cultures;
});

var app = builder.Build();

app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);

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


app.UseSwagger();
app.UseSwaggerUI();


app.Run();

public partial class Program
{
}