using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ServiceTrackingSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceTrackingSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class RouteAssignmentsController : Controller
    {
        private readonly AppDbContext _context;

        public RouteAssignmentsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Admin/RouteAssignments
        public async Task<IActionResult> Index()
        {
            // Daha etkili ve açık bir veri yükleme sağlayalım
            var routeAssignments = await _context.RouteAssignments
                .Include(r => r.Driver)
                .Include(r => r.Employees)
                .AsNoTracking()
                .ToListAsync();

            // Verify employee assignments by checking EmployeeAddresses
            foreach (var assignment in routeAssignments)
            {
                // Find all employees who should be in this route based on EmployeeAddresses
                var employeesWithDriver = await _context.EmployeeAddresses
                    .Where(ea => ea.DriverId == assignment.DriverId && ea.IsActive)
                    .Select(ea => ea.EmployeeId)
                    .ToListAsync();
                
                // Find employees that have this route assignment but not matching DriverId in EmployeeAddresses
                var missingRouteAssignment = assignment.Employees?
                    .Where(e => !employeesWithDriver.Contains(e.Id))
                    .ToList();
                
                // Find employees with DriverId set but without matching RouteAssignment
                var employeesNeedingRouteUpdate = await _context.Employees
                    .Where(e => employeesWithDriver.Contains(e.Id) && 
                           (e.RouteAssignmentId != assignment.Id || e.RouteAssignmentId == null))
                    .ToListAsync();
                
                // Log inconsistencies for debugging
                if (missingRouteAssignment != null && missingRouteAssignment.Any())
                {
                    foreach (var emp in missingRouteAssignment)
                    {
                        Console.WriteLine($"WARNING: Employee {emp.Id} ({emp.Name} {emp.Surname}) " +
                                        $"has RouteAssignment {assignment.Id} but no matching DriverId in EmployeeAddresses");
                    }
                }
                
                if (employeesNeedingRouteUpdate.Any())
                {
                    foreach (var emp in employeesNeedingRouteUpdate)
                    {
                        Console.WriteLine($"WARNING: Employee {emp.Id} ({emp.Name} {emp.Surname}) " +
                                        $"has DriverId {assignment.DriverId} in EmployeeAddresses but " +
                                        $"RouteAssignmentId is {emp.RouteAssignmentId} (should be {assignment.Id})");
                    }
                }
                
                Console.WriteLine($"RouteAssignment ID: {assignment.Id}, " +
                                $"DriverID: {assignment.DriverId}, " +
                                $"EmployeeCount: {assignment.Employees?.Count() ?? 0}, " +
                                $"ExpectedCount: {employeesWithDriver.Count}");
            }

            return View(routeAssignments);
        }

        // GET: Admin/RouteAssignments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var routeAssignment = await _context.RouteAssignments
                .Include(r => r.Driver)
                .Include(r => r.Employees)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (routeAssignment == null)
            {
                return NotFound();
            }

            return View(routeAssignment);
        }

        // GET: Admin/RouteAssignments/Create
        public async Task<IActionResult> Create()
        {
            var drivers = await _context.Drivers
                .Select(d => new { Id = d.Id, FullName = d.Name + " " + d.Surname })
                .ToListAsync();
            
            ViewBag.Drivers = new SelectList(drivers, "Id", "FullName");
            
            var employees = await _context.Employees
                .Where(e => e.RouteAssignmentId == null)
                .Select(e => new EmployeeCheckBoxViewModel
                {
                    Id = e.Id,
                    FullName = e.Name + " " + e.Surname,
                    IsSelected = false
                })
                .ToListAsync();
            
            var viewModel = new RouteAssignmentViewModel
            {
                Employees = employees,
                RouteDate = DateTime.Today
            };
            
            return View(viewModel);
        }

        // POST: Admin/RouteAssignments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RouteAssignmentViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Execution Strategy kullanarak işlemi gerçekleştir
                    await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () => 
                    {
                        var routeAssignment = new RouteAssignment
                        {
                            DriverId = model.DriverId,
                            RouteDate = model.RouteDate
                        };

                        _context.Add(routeAssignment);
                        await _context.SaveChangesAsync();

                        // Debug için seçilen çalışanları logla
                        var selectedEmployeeIds = model.Employees
                            ?.Where(e => e.IsSelected)
                            ?.Select(e => e.Id)
                            ?.ToList() ?? new List<int>();
                        
                        Console.WriteLine($"Yeni Route için seçilen çalışanlar: {string.Join(", ", selectedEmployeeIds)}");

                        // Seçilen çalışanları rotaya ata
                        if (selectedEmployeeIds.Any())
                        {
                            foreach (var employeeId in selectedEmployeeIds)
                            {
                                var employee = await _context.Employees.FindAsync(employeeId);
                                if (employee != null)
                                {
                                    Console.WriteLine($"Çalışan rotaya atanıyor: Id={employee.Id}, Name={employee.Name}");
                                    employee.RouteAssignmentId = routeAssignment.Id;
                                    _context.Update(employee);
                                    
                                    // Also update EmployeeAddresses to synchronize the driver
                                    var employeeAddresses = await _context.EmployeeAddresses
                                        .Where(ea => ea.EmployeeId == employeeId && ea.IsActive)
                                        .ToListAsync();
                                        
                                    foreach (var address in employeeAddresses)
                                    {
                                        address.DriverId = routeAssignment.DriverId;
                                        _context.Update(address);
                                    }
                                }
                            }
                            await _context.SaveChangesAsync();
                        }
                    });

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // Hata durumunu logla
                    Console.WriteLine($"Rota oluşturulurken hata: {ex.Message}");
                    ModelState.AddModelError(string.Empty, $"Rota oluşturulurken bir hata oluştu: {ex.Message}");
                }
            }

            var drivers = await _context.Drivers
                .Select(d => new { Id = d.Id, FullName = d.Name + " " + d.Surname })
                .ToListAsync();
            
            ViewBag.Drivers = new SelectList(drivers, "Id", "FullName", model.DriverId);
            
            return View(model);
        }

        // GET: Admin/RouteAssignments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Düzenlenmek istenen rotayı çalışanlarıyla birlikte yükle
            var routeAssignment = await _context.RouteAssignments
                .Include(r => r.Employees)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (routeAssignment == null)
            {
                return NotFound();
            }

            var drivers = await _context.Drivers
                .Select(d => new { Id = d.Id, FullName = d.Name + " " + d.Surname })
                .ToListAsync();
            
            ViewBag.Drivers = new SelectList(drivers, "Id", "FullName", routeAssignment.DriverId);
            
            // Bu rotaya atanmış çalışanların ID'lerini al
            var assignedEmployeeIds = _context.EmployeeAddresses
                .Where(e => e.DriverId == routeAssignment.DriverId)
                .Select(e => e.Id)
                .ToList() ?? new List<int>();
            
            // Debug için atanmış çalışanları yazdır
            Console.WriteLine($"Route {id} için atanmış çalışanlar: {string.Join(", ", assignedEmployeeIds)}");
            
            // Tüm çalışanları getir (bu rotaya atanmış veya henüz hiçbir rotaya atanmamış)
            var employees = await _context.Employees
                .Where(e => e.RouteAssignmentId == null || e.RouteAssignmentId == id)
                .Select(e => new EmployeeCheckBoxViewModel
                {
                    Id = e.Id,
                    FullName = e.Name + " " + e.Surname,
                    IsSelected = assignedEmployeeIds.Contains(e.Id)
                })
                .ToListAsync();
            
            var viewModel = new RouteAssignmentViewModel
            {
                Id = routeAssignment.Id,
                DriverId = routeAssignment.DriverId,
                RouteDate = routeAssignment.RouteDate,
                Employees = employees
            };
            
            return View(viewModel);
        }

        // POST: Admin/RouteAssignments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RouteAssignmentViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Execution Strategy kullanarak işlemi gerçekleştir
                    await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () => 
                    {
                        var routeAssignment = await _context.RouteAssignments
                            .Include(r => r.Employees)
                            .FirstOrDefaultAsync(r => r.Id == id);
                        
                        if (routeAssignment == null)
                        {
                            return;
                        }

                        routeAssignment.DriverId = model.DriverId;
                        routeAssignment.RouteDate = model.RouteDate;

                        // Debug için seçilen çalışanları logla
                        var selectedEmployeeIds = model.Employees
                            ?.Where(e => e.IsSelected)
                            ?.Select(e => e.Id)
                            ?.ToList() ?? new List<int>();

                        // Clear previous DriverId associations in EmployeeAddresses for this route
                        var previousEmployeeIds = routeAssignment.Employees?.Select(e => e.Id)?.ToList() ?? new List<int>();
                        foreach (var prevEmpId in previousEmployeeIds)
                        {
                            var addresses = await _context.EmployeeAddresses
                                .Where(ea => ea.EmployeeId == prevEmpId && ea.IsActive)
                                .ToListAsync();
                                
                            foreach (var addr in addresses)
                            {
                                if (!selectedEmployeeIds.Contains(prevEmpId))
                                {
                                    addr.DriverId = null;
                                    _context.Update(addr);
                                }
                            }
                        }
                        
                        // Update DriverId for newly selected employees
                        foreach (var selectedEmp in selectedEmployeeIds)
                        {
                            var addresses = await _context.EmployeeAddresses
                                .Where(ea => ea.EmployeeId == selectedEmp && ea.IsActive)
                                .ToListAsync();
                                
                            foreach (var addr in addresses)
                            {
                                addr.DriverId = routeAssignment.DriverId;
                                _context.Update(addr);
                            }
                        }
                        
                        Console.WriteLine($"Route {id} için seçilen çalışanlar: {string.Join(", ", selectedEmployeeIds)}");
                        
                        // Tüm mevcut atanmış çalışanlar için RouteAssignment ilişkisini temizle
                        if (routeAssignment.Employees != null)
                        {
                            foreach (var employee in routeAssignment.Employees.ToList())
                            {
                                Console.WriteLine($"Çalışan ilişkisi temizleniyor: Id={employee.Id}, Name={employee.Name}");
                                employee.RouteAssignmentId = null;
                                _context.Update(employee);
                            }
                        }
                        
                        // Yeni seçilen çalışanları atayalım
                        if (selectedEmployeeIds.Any())
                        {
                            foreach (var employeeId in selectedEmployeeIds)
                            {
                                var employee = await _context.Employees.FindAsync(employeeId);
                                if (employee != null)
                                {
                                    Console.WriteLine($"Çalışan rotaya atanıyor: Id={employee.Id}, Name={employee.Name}");
                                    employee.RouteAssignmentId = routeAssignment.Id;
                                    _context.Update(employee);
                                }
                            }
                        }
                        
                        _context.Update(routeAssignment);
                        await _context.SaveChangesAsync();
                    });
                    
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    // Hatayı logla
                    Console.WriteLine($"Güncelleme esnasında hata: {ex.Message}");
                    
                    if (!RouteAssignmentExists(id))
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
                    Console.WriteLine($"Beklenmeyen hata: {ex.Message}");
                    ModelState.AddModelError(string.Empty, $"Rota güncellenirken bir hata oluştu: {ex.Message}");
                }
            }
            
            // Hata durumunda formu tekrar dolduralım
            var drivers = await _context.Drivers
                .Select(d => new { Id = d.Id, FullName = d.Name + " " + d.Surname })
                .ToListAsync();
            
            ViewBag.Drivers = new SelectList(drivers, "Id", "FullName", model.DriverId);
            
            return View(model);
        }

        // GET: Admin/RouteAssignments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var routeAssignment = await _context.RouteAssignments
                .Include(r => r.Driver)
                .Include(r => r.Employees)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (routeAssignment == null)
            {
                return NotFound();
            }

            return View(routeAssignment);
        }

        // POST: Admin/RouteAssignments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var routeAssignment = await _context.RouteAssignments
                .Include(r => r.Employees)
                .FirstOrDefaultAsync(r => r.Id == id);
            
            if (routeAssignment != null)
            {
                // Reset route assignment for all assigned employees
                foreach (var employee in routeAssignment.Employees)
                {
                    employee.RouteAssignmentId = null;
                    _context.Update(employee);
                }
                
                _context.RouteAssignments.Remove(routeAssignment);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool RouteAssignmentExists(int id)
        {
            return _context.RouteAssignments.Any(e => e.Id == id);
        }
    }

    public class RouteAssignmentViewModel
    {
        public int Id { get; set; }
        public int DriverId { get; set; }
        public DateTime RouteDate { get; set; }
        public List<EmployeeCheckBoxViewModel> Employees { get; set; } = new List<EmployeeCheckBoxViewModel>();
    }

    public class EmployeeCheckBoxViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public bool IsSelected { get; set; }
    }
} 