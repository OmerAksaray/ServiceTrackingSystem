using Microsoft.AspNetCore.Identity;
using ServiceTrackingSystem.Models;

namespace ServiceTrackingSystem.Areas.Admin
{
    public static class AdminSeedData
    {
        public static async Task SeedAdminUser(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<int>> roleManager)
        {
            // Create Admin role if it doesn't exist
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole<int>("Admin"));
            }

            // Create Employee role if it doesn't exist
            if (!await roleManager.RoleExistsAsync("Employee"))
            {
                await roleManager.CreateAsync(new IdentityRole<int>("Employee"));
            }

            // Create Driver role if it doesn't exist
            if (!await roleManager.RoleExistsAsync("Driver"))
            {
                await roleManager.CreateAsync(new IdentityRole<int>("Driver"));
            }

            // Create admin user if it doesn't exist
            var adminEmail = "admin@servicetracking.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    Name = "Admin",
                    Surname = "User",
                    UserType = "Admin",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123!");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
            else if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }
} 