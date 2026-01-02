var builder = WebApplication.CreateBuilder(args);

// 1. Tambahkan Service MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// 2. Konfigurasi Error Handling
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

// 3. PENTING: Jangan pakai HttpsRedirection saat development lokal
// app.UseHttpsRedirection(); 

app.UseStaticFiles(); // Ini wajib ada untuk memuat CSS/JS

app.UseRouting();

app.UseAuthorization();

// 4. Routing Standar (JANGAN PAKAI .WithStaticAssets)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();