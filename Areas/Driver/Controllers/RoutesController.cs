using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceTrackingSystem.Models; 

namespace ServiceTrackingSystem.Areas.Driver.Controllers
{
    [Area("Driver")]
    public class RoutesController : Controller
    {
        private readonly AppDbContext _context;

        public RoutesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Driver/Routes
        public async Task<IActionResult> Index()
        {
            var userId = Int32.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (userId == null)
                return Challenge();  


            var assignmentsIds = await _context.EmployeeAddresses
                .Where(r => r.IsActive == true && r.Driver.Id == userId)
                .Select(y => y.LocationId)
                .ToListAsync();

            var locations = await _context.Locations
                .Where(x => assignmentsIds.Contains(x.Id))
                .ToListAsync();

            return View(locations);
        }
    }
}
