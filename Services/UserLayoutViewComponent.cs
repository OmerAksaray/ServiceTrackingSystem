using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ServiceTrackingSystem.Models;
using System.Threading.Tasks;
using System.Security.Claims;

namespace ServiceTrackingSystem.Services
{
    public class UserLayoutViewComponent : ViewComponent
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserLayoutViewComponent(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (User.Identity.IsAuthenticated)
            {
                // Cast User to ClaimsPrincipal explicitly
                var claimsPrincipal = User as ClaimsPrincipal;
                if (claimsPrincipal != null)
                {
                    ApplicationUser user = await _userManager.GetUserAsync(claimsPrincipal);
                    if (user != null)
                    {
                        ViewBag.UserType = user.UserType;
                        return View("Default", user.UserType);
                    }
                }
            }

            return View("Default", "Anonymous");
        }
    }
}
