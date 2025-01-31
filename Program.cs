using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using ServiceTrackingSystem.Models;
using ServiceTrackingSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// 📌 Veritabanı Bağlantısı
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 📌 Kimlik Doğrulama
builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>() 
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();


// 📌 Çerez (Cookie) Yapılandırması
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";  // Identity sayfasını kullanıyoruz
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
});

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromHours(3); // Şifre sıfırlama linkinin geçerlilik süresi
});

// 📌 MVC ve Razor Pages Ekleme
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// 📌 Hata Sayfası ve HSTS (Production Modu İçin)
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// 📌 Middleware Kullanımı
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// 📌 Varsayılan Route Tanımlama
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages(); // 📌 Identity'nin Razor Pages sayfalarını ekliyoruz

app.Run();
