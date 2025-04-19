using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ServiceTrackingSystem.Models;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceTrackingSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class EmployeesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;

        public EmployeesController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<int>> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Admin/Employees
        public async Task<IActionResult> Index()
        {
            var employees = await _context.Employees
                .Include(e => e.Driver)
                .Include(e => e.RouteAssignment)
                .Include(e => e.Addresses.Where(a => a.IsActive))
                .ToListAsync();
            
            // Get driver information for each employee from active addresses
            foreach (var employee in employees)
            {
                // If the employee already has a driver via RouteAssignment, use that
                if (employee.RouteAssignment != null && employee.RouteAssignment.Driver != null)
                {
                    employee.Driver = employee.RouteAssignment.Driver;
                    employee.DriverId = employee.RouteAssignment.DriverId;
                }
                // Otherwise, try to get driver info from active addresses
                else
                {
                    var activeAddress = await _context.EmployeeAddresses
                        .Where(x => x.EmployeeId == employee.Id && x.IsActive == true)
                        .FirstOrDefaultAsync();
                        
                    if (activeAddress != null && activeAddress.DriverId.HasValue)
                    {
                        employee.DriverId = activeAddress.DriverId;
                        if (employee.DriverId.HasValue)
                        {
                            employee.Driver = await _context.Drivers.FindAsync(employee.DriverId);
                        }
                    }
                }
                
                // Log for debugging
                Console.WriteLine($"Employee: {employee.Name} {employee.Surname}, " +
                                 $"DriverId: {employee.DriverId}, " +
                                 $"RouteAssignmentId: {employee.RouteAssignmentId}");
            }
            
            return View(employees);
        }

        // GET: Admin/Employees/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .Include(e => e.Driver)
                .Include(e => e.RouteAssignment)
                .Include(e => e.Addresses)
                .ThenInclude(a => a.Location)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // GET: Admin/Employees/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Employees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Surname,Email,PhoneNumber,Password")] CreateEmployeeViewModel model)
        {
            if (ModelState.IsValid)
            {
                var employee = new ServiceTrackingSystem.Models.Employee
                {
                    UserName = model.Email,
                    Email = model.Email,
                    Name = model.Name,
                    Surname = model.Surname,
                    PhoneNumber = model.PhoneNumber,
                    UserType = "Employee"
                };

                var result = await _userManager.CreateAsync(employee, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(employee, "Employee");
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        // GET: Admin/Employees/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .Include(e => e.Driver)
                .FirstOrDefaultAsync(e => e.Id == id);
                
            if (employee == null)
            {
                return NotFound();
            }

            // Get list of all drivers for the dropdown
            var drivers = await _context.Drivers
                .Select(d => new { Id = d.Id, FullName = d.Name + " " + d.Surname })
                .ToListAsync();
            
            ViewBag.Drivers = new SelectList(drivers, "Id", "FullName", employee.DriverId);

            var driverId = _context.EmployeeAddresses.Where(x => x.EmployeeId == employee.Id && x.IsActive == true).FirstOrDefaultAsync().Result.DriverId;

            var driverData = _context.Users.Where(x => x.DriverId == driverId).FirstOrDefaultAsync().Result;

            var viewModel = new EditEmployeeViewModel
            {
                Id = employee.Id,
                Name = employee.Name,
                Surname = employee.Surname,
                Email = employee.Email,
                PhoneNumber = employee.PhoneNumber,
                DriverId = driverId
            };

            return View(viewModel);
        }

        // POST: Admin/Employees/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Surname,Email,PhoneNumber,DriverId")] EditEmployeeViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Execution strategy kullanarak işlemleri gerçekleştir
                    await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
                    {
                        var employee = await _context.Employees.FindAsync(id);
                        if (employee == null)
                        {
                            return;
                        }

                        // Değerleri güncelle
                        employee.Name = model.Name;
                        employee.Surname = model.Surname;
                        employee.Email = model.Email;
                        employee.PhoneNumber = model.PhoneNumber;
                        
                        var employeeAddress = _context.EmployeeAddresses.ToList().Where(x => x.EmployeeId == id);

                        foreach (var address in employeeAddress)
                        {
                            address.DriverId = model.DriverId;
                        }
                        
                        // Update the RouteAssignment relationship if driver changed
                        if (model.DriverId.HasValue)
                        {
                            // Get the route assignment for this driver
                            var driverRouteAssignment = await _context.RouteAssignments
                                .FirstOrDefaultAsync(r => r.DriverId == model.DriverId);
                                
                            // If the driver has a route assignment, assign the employee to it
                            if (driverRouteAssignment != null)
                            {
                                employee.RouteAssignmentId = driverRouteAssignment.Id;
                            }
                        }
                        else
                        {
                            // If no driver is assigned, clear the route assignment
                            employee.RouteAssignmentId = null;
                        }
                        
                        employee.UpdatedDate = DateTime.UtcNow;

                        // Debug için log
                        Console.WriteLine($"Saving Employee: ID={employee.Id}, DriverId={model.DriverId}, RouteAssignmentId={employee.RouteAssignmentId}");

                        _context.Update(employee);
                        await _context.SaveChangesAsync();
                    });
                    
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // Hatayı logla
                    Console.WriteLine($"Concurrency Exception: {ex.Message}");
                    
                    if (!EmployeeExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    // Genel hataları logla
                    Console.WriteLine($"Exception during save: {ex.Message}");
                    throw;
                }
            }
            
            // Hata durumunda şoför listesini yeniden oluştur
            var drivers = await _context.Drivers
                .Select(d => new { Id = d.Id, FullName = d.Name + " " + d.Surname })
                .ToListAsync();
            
            ViewBag.Drivers = new SelectList(drivers, "Id", "FullName", model.DriverId);
            
            return View(model);
        }

        // GET: Admin/Employees/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // POST: Admin/Employees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.Id == id);
        }
    }

    public class CreateEmployeeViewModel
    {
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
    }

    public class EditEmployeeViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public int? DriverId { get; set; }
    }
} 