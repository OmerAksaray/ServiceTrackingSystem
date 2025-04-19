using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceTrackingSystem.Models;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceTrackingSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DriversController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;

        public DriversController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<int>> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Admin/Drivers
        public async Task<IActionResult> Index()
        {
            var drivers = await _context.Drivers
                .Include(d => d.RouteAssignment)
                .ThenInclude(r => r.Employees)
                .ToListAsync();
            return View(drivers);
        }

        // GET: Admin/Drivers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var driver = await _context.Drivers
                .Include(d => d.RouteAssignment)
                .ThenInclude(r => r.Employees)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (driver == null)
            {
                return NotFound();
            }

            return View(driver);
        }

        // GET: Admin/Drivers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Drivers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Surname,Email,PhoneNumber,LicenseNumber,Password")] CreateDriverViewModel model)
        {
            if (ModelState.IsValid)
            {
                var driver = new ServiceTrackingSystem.Models.Driver
                {
                    UserName = model.Email,
                    Email = model.Email,
                    Name = model.Name,
                    Surname = model.Surname,
                    PhoneNumber = model.PhoneNumber,
                    LicenseNumber = model.LicenseNumber,
                    UserType = "Driver"
                };

                var result = await _userManager.CreateAsync(driver, model.Password);
                
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(driver, "Driver");
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        // GET: Admin/Drivers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var driver = await _context.Drivers.FindAsync(id);
            if (driver == null)
            {
                return NotFound();
            }
            
            var viewModel = new EditDriverViewModel
            {
                Id = driver.Id,
                Name = driver.Name,
                Surname = driver.Surname,
                Email = driver.Email,
                PhoneNumber = driver.PhoneNumber,
                LicenseNumber = driver.LicenseNumber
            };
            
            return View(viewModel);
        }

        // POST: Admin/Drivers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Surname,Email,PhoneNumber,LicenseNumber")] EditDriverViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var driver = await _context.Drivers.FindAsync(id);
                    if (driver == null)
                    {
                        return NotFound();
                    }

                    driver.Name = model.Name;
                    driver.Surname = model.Surname;
                    driver.Email = model.Email;
                    driver.PhoneNumber = model.PhoneNumber;
                    driver.LicenseNumber = model.LicenseNumber;
                    driver.UpdatedDate = DateTime.UtcNow;

                    _context.Update(driver);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DriverExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // GET: Admin/Drivers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var driver = await _context.Drivers
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (driver == null)
            {
                return NotFound();
            }

            return View(driver);
        }

        // POST: Admin/Drivers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var driver = await _context.Drivers.FindAsync(id);
            if (driver != null)
            {
                _context.Drivers.Remove(driver);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool DriverExists(int id)
        {
            return _context.Drivers.Any(e => e.Id == id);
        }
    }

    public class CreateDriverViewModel
    {
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string LicenseNumber { get; set; }
        public string Password { get; set; }
    }

    public class EditDriverViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string LicenseNumber { get; set; }
    }
} 