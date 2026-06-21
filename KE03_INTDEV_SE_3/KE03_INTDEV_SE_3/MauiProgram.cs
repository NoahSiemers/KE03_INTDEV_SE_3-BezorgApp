using KE03_INTDEV_SE_3.Data;
using KE03_INTDEV_SE_3.Helpers;
using KE03_INTDEV_SE_3.Pages;
using KE03_INTDEV_SE_3.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KE03_INTDEV_SE_3;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

#if ANDROID || IOS || MACCATALYST
        builder.UseMauiMaps();
#endif

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("Font Awesome 7 Free-Solid-900.otf", "FontAwesome");
            });

        string connectionString =
            "Server=(localdb)\\MSSQLLocalDB;Database=MatrixIncRittenDb;Trusted_Connection=True;TrustServerCertificate=True;";

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        builder.Services.AddSingleton<AppState>();

        builder.Services.AddSingleton(new HttpClient());
        builder.Services.AddTransient<NavigationService>();

        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<HelpPage>();
        builder.Services.AddTransient<TodayRidePage>();
        builder.Services.AddTransient<MyRidesPage>();
        builder.Services.AddTransient<AccountPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        ServiceHelper.Services = app.Services;

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
            DatabaseSeeder.Seed(db);
        }

        return app;
    }
}