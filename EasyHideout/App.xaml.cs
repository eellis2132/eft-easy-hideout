using System.Windows;
using EasyHideout.Data;
using EasyHideout.Models;
using EasyHideout.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EasyHideout;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        services.AddSingleton<TooltipService>();
        services.AddSingleton<TarkovApiService>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<DistributionService>();
        services.AddDbContext<AppDbContext>(ServiceLifetime.Transient);
        ServiceLocator.Initialize(services.BuildServiceProvider());

        InitializeDatabase();
        ServiceLocator.Get<ThemeService>().LoadFromDb();
    }

    private static void InitializeDatabase()
    {
        using var db = new AppDbContext();
        db.Database.Migrate();

        if (!db.Profiles.Any())
        {
            db.Profiles.AddRange(
                new Profile { Name = "PvE" },
                new Profile { Name = "PvP" }
            );
            db.SaveChanges();
        }

        if (!db.AppSettings.Any())
        {
            var firstProfile = db.Profiles.First();
            db.AppSettings.Add(new AppSettings { Id = 1, ActiveProfileId = firstProfile.Id });
            db.SaveChanges();
        }
    }
}
