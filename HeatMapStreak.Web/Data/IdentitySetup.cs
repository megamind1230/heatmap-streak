using Microsoft.AspNetCore.Identity;

namespace HeatMapStreak.Web.Data;

/// <summary>
/// Single source of truth for the Identity options used by the running app, so tests
/// exercise exactly the same policy instead of a copy that can drift apart.
/// </summary>
public static class IdentitySetup
{
    public static void Configure(IdentityOptions options)
    {
        options.SignIn.RequireConfirmedAccount = false;

        // Identity's defaults require an uppercase letter, a digit and a symbol, which the
        // seeded development account (see <see cref="DbSeeder"/>) does not have. Relaxing
        // these applies to every registration on the site, not just the seeded account.
        options.Password.RequireUppercase = false;
        options.Password.RequireDigit = false;
        options.Password.RequireNonAlphanumeric = false;
    }
}
