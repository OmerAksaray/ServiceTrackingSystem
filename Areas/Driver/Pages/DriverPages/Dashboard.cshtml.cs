using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ServiceTrackingSystem.Models;

namespace ServiceTrackingSystem.Areas.Driver.Pages.DriverPages
{
    [Area("Driver")]
    public class DashboardModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardModel(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Check if the user is a driver
            if (user.UserType != "DRIVER")
            {
                return RedirectToPage("/Account/AccessDenied", new { area = "Identity" });
            }

            // Load driver-specific data here if needed

            return Page();
        }
    }
}
