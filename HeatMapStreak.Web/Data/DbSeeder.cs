using Microsoft.AspNetCore.Identity;

namespace HeatMapStreak.Web.Data;

/// <summary>
/// Creates the local development account on startup so a fresh clone can be used
/// straight away. Never runs outside the Development environment.
/// </summary>
public static class DbSeeder
{
    public const string DefaultEmail = "hassan24641@gmail.com";
    public const string DefaultPassword = "heatmapstreak";

    /// <summary>
    /// Ensures the seed account exists. Idempotent: returns <c>false</c> when the
    /// account was already present, <c>true</c> when it was created by this call.
    /// </summary>
    /// <remarks>
    /// The password goes through Identity's password validator, so it must satisfy the
    /// policy in <see cref="IdentitySetup.Configure"/> (uppercase/digit/symbol off).
    /// </remarks>
    public static async Task<bool> SeedAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger logger)
    {
        var email = configuration["Seed:AdminEmail"] ?? DefaultEmail;
        var password = configuration["Seed:AdminPassword"] ?? DefaultPassword;

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            // Make sure the account can sign in immediately, no matter how it was created.
            if (!existing.EmailConfirmed)
            {
                existing.EmailConfirmed = true;
                await userManager.UpdateAsync(existing);
            }

            logger.LogInformation("Seed account {Email} already exists; skipping.", email);
            return false;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            logger.LogError(
                "Failed to seed account {Email}: {Errors}",
                email,
                string.Join("; ", result.Errors.Select(e => e.Description)));
            return false;
        }

        logger.LogInformation("Seeded development account {Email}.", email);
        return true;
    }
}
