using IT_Destek_Panel.Hubs;
using IT_Destek_Panel.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

// 1. Controller ve View desteğini ekliyoruz
builder.Services.AddControllersWithViews();

// 2. Veritabanı bağlantısı (appsettings.json içindeki DefaultConnection'ı okur)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. Admin/User ayrımı için Çerez (Cookie) bazlı kimlik doğrulama ayarları
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", config =>
    {
        config.Cookie.Name = "ITHelpdeskLogin";
        config.LoginPath = "/Account/Login"; // Giriş yapmayan buraya atılır
        config.AccessDeniedPath = "/Account/AccessDenied"; // Yetkisi olmayan (User'ın Admin'e girmeye çalışması gibi) buraya atılır
        config.ExpireTimeSpan = TimeSpan.FromHours(24); // 24 saat boyunca açık kalır
    });

var app = builder.Build();

// HTTP İstek Hattını Yapılandırma (Middleware Pipeline)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Özel 404 Hata Sayfası Yönlendirmesi
app.UseStatusCodePagesWithReExecute("/Account/NotFoundPage");

app.UseRouting();

// ÖNEMLİ: Kimlik doğrulama her zaman yetkilendirmeden önce gelir
app.UseAuthentication();
app.UseAuthorization();

// Statik dosyaları (CSS, JS, Resimler) haritalar
app.MapStaticAssets();

app.MapHub<TicketHub>("/ticketHub");
// Varsayılan Başlangıç Sayfası: Account/Login
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();