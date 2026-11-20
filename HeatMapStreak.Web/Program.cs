using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HeatMapStreak.Web.Components;
using HeatMapStreak.Web.Components.Account;
using HeatMapStreak.Web.Data;
using HeatMapStreak.Web.Logging;
using HeatMapStreak.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

var authBuilder = builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    });

authBuilder.AddIdentityCookies();

var googleId = builder.Configuration["Authentication:Google:ClientId"];
if (!string.IsNullOrEmpty(googleId))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleId;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
    });
}

var githubId = builder.Configuration["Authentication:GitHub:ClientId"];
if (!string.IsNullOrEmpty(githubId))
{
    authBuilder.AddGitHub(options =>
    {
        options.ClientId = githubId;
        options.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"] ?? "";
        options.Scope.Add("user:email");
    });
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(IdentitySetup.Configure)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

builder.Services.AddScoped<IHabitService, HabitService>();
builder.Services.AddScoped<IStreakService, StreakService>();

builder.Logging.AddFileLogger(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    // Development only: make sure the local seed account exists so a fresh clone is
    // immediately usable without registering through the UI first.
    if (app.Environment.IsDevelopment())
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await DbSeeder.SeedAsync(userManager, app.Configuration, app.Logger);
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();

app.Run();
