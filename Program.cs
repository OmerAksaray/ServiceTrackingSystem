using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using ServiceTrackingSystem.Areas.Admin;
using ServiceTrackingSystem.Models;
using ServiceTrackingSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// 📌 Veritabanı Bağlantısı
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptionsAction: sqlOptions => 
        {
            sqlOptions.CommandTimeout(600); // Increase command timeout to 10 minutes
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        }));

// 📌 Kimlik Doğrulama
builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>() 
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// Add claims principal factory
builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, UserClaimsPrincipalFactory>();

// HttpClient Factory ekleyelim - API istekleri için
builder.Services.AddHttpClient();

// Register Yandex Geocoder (add logger to the registration)
builder.Services.AddHttpClient<YandexGeocoderService>();

// Register our Google Maps services
builder.Services.AddHttpClient<GoogleGeocodingService>();
builder.Services.AddHttpClient<GoogleRoutingService>();
builder.Services.AddScoped<IGeocodingService, GoogleGeocodingService>();
builder.Services.AddScoped<IRoutingService, GoogleRoutingService>();


// 📌 Çerez (Cookie) Yapılandırması
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";  // Identity sayfasını kullanıyoruz
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.LogoutPath = "/Identity/Account/Logout";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.SlidingExpiration = true;
    // ReturnUrlParameter'i boş bırakırsak, giriş sonrasında varsayılan yönlendirmeyi dikkate almayacak
    // böylece Login.cshtml.cs'deki yönlendirme mantığımızı çalıştıracaktır
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

// Seed admin user
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
        await AdminSeedData.SeedAdminUser(userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database with admin user.");
    }
}

// 📌 Hata Sayfası ve HSTS (Production Modu İçin)
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    // BrowserLink'i devre dışı bırakarak CORS hatalarını önlüyoruz
    // app.UseBrowserLink(); 
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

// Add our custom middleware to handle user type-based layouts
app.UseUserTypeLayout();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);
// 📌 Varsayılan Route Tanımlama
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages(); // 📌 Identity'nin Razor Pages sayfalarını ekliyoruz

app.Run();
