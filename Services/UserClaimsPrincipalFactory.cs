using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ServiceTrackingSystem.Models;
using System.Security.Claims;

namespace ServiceTrackingSystem.Services
{
    public class UserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole<int>>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<int>> roleManager,
            IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, roleManager, optionsAccessor)
        {
            _userManager = userManager;
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            var identity = await base.GenerateClaimsAsync(user);
            
            // Add UserType claim
            if (!string.IsNullOrEmpty(user.UserType))
            {
                identity.AddClaim(new Claim("UserType", user.UserType));
            }

            return identity;
        }
    }
}
