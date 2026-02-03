using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HcPortal.Data;
using HcPortal.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Tambahkan Service MVC
builder.Services.AddControllersWithViews();

// 2. Konfigurasi Database (SQLite)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// 4. Konfigurasi Cookie Authentication
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8); // Session 8 jam
    options.SlidingExpiration = true;
});

var app = builder.Build();

// 5. Initialize Database & Seed Data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate(); // Apply migrations
        await SeedData.InitializeAsync(services); // Seed roles & users
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
}

// 7. PENTING: Jangan pakai HttpsRedirection saat development lokal
// app.UseHttpsRedirection(); 

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

app.UseStaticFiles(staticFileOptions); // Ini wajib ada untuk memuat CSS/JS dan PDF

app.UseRouting();

// 8. PENTING: Authentication harus sebelum Authorization
app.UseAuthentication();
app.UseAuthorization();

// 9. Routing Standar
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();