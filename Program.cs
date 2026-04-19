using IT_Destek_Panel.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// 1. Veritabanı bağlantımızı sisteme tanıtıyoruz
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Admin/User ayrımı için çerez (Cookie) bazlı kimlik doğrulama ekliyoruz
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", config =>
    {
        config.Cookie.Name = "ITHelpdeskLogin";
        config.LoginPath = "/Account/Login"; // Giriş yapmayan biri sayfaya girmeye çalışırsa buraya atılacak
        config.AccessDeniedPath = "/Account/AccessDenied"; // Yetkisi olmayan (örneğin admin sayfasına girmeye çalışan User) buraya atılacak
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
// --- ÖZEL 404 HATA SAYFASI YÖNLENDİRMESİ ---
app.UseStatusCodePagesWithReExecute("/Account/NotFoundPage");

app.UseAuthentication(); // Oturum açma kontrolünü aktifleştirir
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();


app.Run();
