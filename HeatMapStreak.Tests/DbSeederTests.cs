using HeatMapStreak.Web.Data;
using HeatMapStreak.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HeatMapStreak.Tests;

public class DbSeederTests : IDisposable
{
    private readonly TestDatabase _db = new();

    public void Dispose() => _db.Dispose();

    private static IConfiguration BuildConfig(string email, string password) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Seed:AdminEmail"] = email,
                ["Seed:AdminPassword"] = password
            })
            .Build();

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_db.ConnectionString));
        services.AddIdentityCore<ApplicationUser>(IdentitySetup.Configure)
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // The schema itself is created by TestDatabase via the real migrations.
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Seed_CreatesTheLowercaseSeedAccount()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var created = await DbSeeder.SeedAsync(
            userManager, BuildConfig("dev@example.com", "heatmapstreak"), NullLogger.Instance);

        Assert.True(created);

        var user = await userManager.FindByEmailAsync("dev@example.com");
        Assert.NotNull(user);
        Assert.True(user!.EmailConfirmed);

        // Must pass the password policy configured in Program.cs (no upper/digit/symbol).
        Assert.True(await userManager.CheckPasswordAsync(user, "heatmapstreak"));
    }

    [Fact]
    public async Task Seed_IsIdempotent()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var config = BuildConfig("dev@example.com", "heatmapstreak");

        Assert.True(await DbSeeder.SeedAsync(userManager, config, NullLogger.Instance));
        Assert.False(await DbSeeder.SeedAsync(userManager, config, NullLogger.Instance));

        await using var db = _db.CreateContext();
        Assert.Equal(1, await db.Users.CountAsync(u => u.Email == "dev@example.com"));
    }

    [Fact]
    public async Task Seed_UsesTheDefaultEmail_WhenNoOverrideIsConfigured()
    {
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await DbSeeder.SeedAsync(
            userManager, new ConfigurationBuilder().Build(), NullLogger.Instance);

        Assert.NotNull(await userManager.FindByEmailAsync(DbSeeder.DefaultEmail));
    }
}
