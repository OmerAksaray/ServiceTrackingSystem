using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ServiceTrackingSystem.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;
using System.Diagnostics;

namespace ServiceTrackingSystem.Services
{
    public class UserTypeLayoutMiddleware
    {
        private readonly RequestDelegate _next;

        public UserTypeLayoutMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User.Identity.IsAuthenticated)
            {
                // Önce claims'den UserType'i almaya çalış
                var claimsPrincipal = context.User as ClaimsPrincipal;
                string userType = null;
                
                if (claimsPrincipal != null)
                {
                    // Öncelikle claims'den UserType'i kontrol et
                    var userTypeClaim = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == "UserType");
                    if (userTypeClaim != null)
                    {
                        userType = userTypeClaim.Value;
                    }
                    
                    // Eğer claims'de bulunamazsa ve çok gerekliyse veritabanından getir
                    // Not: Bu önemli performans sorunlarına neden olabilir, sadece gerekliyse kullan
                    if (string.IsNullOrEmpty(userType))
                    {
                        var userManager = context.RequestServices.GetService<UserManager<ApplicationUser>>();
                        if (userManager != null)
                        {
                            try
                            {
                                // Veritabanı sorgusu için timeout ekle
                                var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                                var user = await userManager.GetUserAsync(claimsPrincipal).ConfigureAwait(false);

                                if (user != null)
                                {
                                    userType = user.UserType;
                                }
                            }
                            catch (Exception ex)
                            {
                                // Hata durumunda logla, ama uygulamanın çalışmaya devam etmesine izin ver
                                // Gerçek bir uygulama için uygun loglama eklenebilir
                                Debug.WriteLine($"UserType retrieval error: {ex.Message}");
                            }
                        }
                    }
                }
                
                // UserType'i bulduk, HttpContext.Items'a ekle
                if (!string.IsNullOrEmpty(userType))
                {
                    context.Items["UserType"] = userType;
                }
            }

            await _next(context);
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class UserTypeLayoutMiddlewareExtensions
    {
        public static IApplicationBuilder UseUserTypeLayout(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<UserTypeLayoutMiddleware>();
        }
    }
}
