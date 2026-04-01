using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HcPortal.Data;
using HcPortal.Models;
using HcPortal.Hubs;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// 1. Tambahkan Service MVC
builder.Services.AddControllersWithViews();

// Session configuration for TempData
builder.Services.AddDistributedMemoryCache();
builder.Services.AddMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 2. Konfigurasi Database (SQL Server untuk Development dan Production)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 3. Konfigurasi Identity dengan password sederhana untuk development
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password requirements yang sederhana untuk development
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    
    // User settings
    options.User.RequireUniqueEmail = true;
    
    // Sign in settings
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Audit log service
builder.Services.AddScoped<HcPortal.Services.AuditLogService>();

// Impersonation service — Phase 283
builder.Services.AddScoped<HcPortal.Services.ImpersonationService>();
builder.Services.AddHttpContextAccessor();

// Notification Service — Phase 99
builder.Services.AddScoped<HcPortal.Services.INotificationService, HcPortal.Services.NotificationService>();
builder.Services.AddScoped<HcPortal.Services.IWorkerDataService, HcPortal.Services.WorkerDataService>();

// Auth service — factory delegates based on Authentication:UseActiveDirectory config toggle
// dev (false) -> LocalAuthService (Identity PasswordHash)
// prod (true)  -> HybridAuthService (AD for all users, local fallback for admin@pertamina.com)
// Environment variable override: Authentication__UseActiveDirectory=true
var useActiveDirectory = builder.Configuration.GetValue<bool>("Authentication:UseActiveDirectory", false);
if (useActiveDirectory)
{
    // Hybrid mode: AD for all users, local fallback for admin@pertamina.com only
    builder.Services.AddScoped<HcPortal.Services.IAuthService>(sp =>
        new HcPortal.Services.HybridAuthService(
            new HcPortal.Services.LdapAuthService(
                sp.GetRequiredService<IConfiguration>(),
                sp.GetRequiredService<ILogger<HcPortal.Services.LdapAuthService>>()
            ),
            new HcPortal.Services.LocalAuthService(
                sp.GetRequiredService<SignInManager<ApplicationUser>>(),
                sp.GetRequiredService<ILogger<HcPortal.Services.LocalAuthService>>()
            ),
            sp.GetRequiredService<ILogger<HcPortal.Services.HybridAuthService>>()
        )
    );
}
else
{
    builder.Services.AddScoped<HcPortal.Services.IAuthService>(sp =>
        new HcPortal.Services.LocalAuthService(
            sp.GetRequiredService<SignInManager<ApplicationUser>>(),
            sp.GetRequiredService<ILogger<HcPortal.Services.LocalAuthService>>()
        )
    );
}

// 4. Konfigurasi Cookie Authentication
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8); // Session 8 jam
    options.SlidingExpiration = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    // Return 401 for SignalR hub endpoints instead of redirecting to login page
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/hubs"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        }
        else
        {
            context.Response.Redirect(context.RedirectUri);
        }
        return Task.CompletedTask;
    };
});

// SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// 5. Initialize Database & Seed Data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate(); // Apply migrations
        await SeedData.InitializeAsync(services, app.Environment); // Seed roles & users

        // Activate WAL mode for SQLite to allow concurrent reads during SignalR writes
        if (context.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
            var mode = await context.Database.SqlQueryRaw<string>("PRAGMA journal_mode;").FirstOrDefaultAsync();
            logger.LogInformation("SQLite journal mode: {Mode}", mode);
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// 6. Konfigurasi Error Handling
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Handle 404 dan status code errors lainnya
app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

// 7. HTTPS Redirection — aktif di production, skip saat development lokal
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}


// Konfigurasi Static Files dengan MIME type yang benar untuk PDF
var staticFileOptions = new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Set header untuk PDF agar ditampilkan inline, bukan download
        if (ctx.File.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Context.Response.Headers.Append("Content-Disposition", "inline");
            ctx.Context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        }
    }
};

var pathBase = builder.Configuration.GetValue<string>("PathBase");
if (!string.IsNullOrEmpty(pathBase))
{
    app.UsePathBase(pathBase);
}

app.UseStaticFiles(staticFileOptions); // Ini wajib ada untuk memuat CSS/JS dan PDF

app.UseRouting();

app.UseSession();

// 8. PENTING: Authentication harus sebelum Authorization
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<HcPortal.Middleware.ImpersonationMiddleware>();

// 9. Routing Standar
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.MapHub<AssessmentHub>("/hubs/assessment");

app.Run();