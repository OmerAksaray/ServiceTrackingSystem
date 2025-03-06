using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using ServiceTrackingSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ServiceTrackingSystem.Areas.Employee.Pages.EmployeePages
{
    public class AddressManagerModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpClientFactory _clientFactory;

        public AddressManagerModel(AppDbContext context, UserManager<ApplicationUser> userManager, IHttpClientFactory clientFactory)
        {
            _context = context;
            _userManager = userManager;
            _clientFactory = clientFactory;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public List<EmployeeAddress> EmployeeAddresses { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            // Önce kullanıcının employee kaydını bulalım
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == user.Id);

            if (employee == null)
            {
                StatusMessage = "Error! Employee record not found.";
                return Page();
            }

            // Kullanıcının tüm adreslerini getir
            EmployeeAddresses = await _context.EmployeeAddresses
                .Include(e => e.Location)
                .Where(a => a.EmployeeId == employee.Id)
                .OrderByDescending(a => a.IsActive)
                .ThenByDescending(a => a.CreatedDate)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostSetActiveAsync(int addressId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            // Önce kullanıcının employee kaydını bulalım
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == user.Id);

            if (employee == null)
            {
                StatusMessage = "Error! Employee record not found.";
                return Page();
            }

            // Kullanıcının tüm adreslerini getir
            var addresses = await _context.EmployeeAddresses
                .Where(a => a.EmployeeId == employee.Id)
                .ToListAsync();

            var selectedAddress = addresses.FirstOrDefault(a => a.Id == addressId);
            if (selectedAddress == null)
            {
                StatusMessage = "Error! Selected address not found.";
                return RedirectToPage();
            }

            // Önce tüm adresleri pasif yap
            foreach (var address in addresses)
            {
                address.IsActive = false;
            }

            // Seçilen adresi aktif yap
            selectedAddress.IsActive = true;
            selectedAddress.UpdatedDate = DateTime.UtcNow;

            // Değişiklikleri kaydet
            await _context.SaveChangesAsync();

            StatusMessage = "Active address successfully updated.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int addressId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            // Find the employee record
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == user.Id);

            if (employee == null)
            {
                StatusMessage = "Error! Employee record not found.";
                return Page();
            }
            
            // Find the address with its associated location
            var address = await _context.EmployeeAddresses
                .Include(e => e.Location)
                .FirstOrDefaultAsync(a => a.Id == addressId && a.EmployeeId == employee.Id);

            if (address == null)
            {
                StatusMessage = "Error! Address to delete not found.";
                return RedirectToPage();
            }

            // If deleting the active address, handle setting a new active address
            if (address.IsActive)
            {
                // Check if there are other addresses
                var otherAddresses = await _context.EmployeeAddresses
                    .Where(a => a.EmployeeId == employee.Id && a.Id != addressId)
                    .ToListAsync();

                if (otherAddresses.Any())
                {
                    // Set another address as active
                    var newActiveAddress = otherAddresses.First();
                    newActiveAddress.IsActive = true;
                    newActiveAddress.UpdatedDate = DateTime.UtcNow;
                    StatusMessage = "Your active address was deleted. Another address has been automatically set as active.";
                }
                else
                {
                    StatusMessage = "Warning! You deleted your active address and you don't have any other addresses.";
                }
            }

            // Store the location to delete it after removing the address
            var locationToDelete = address.Location;

            // Delete the address
            _context.EmployeeAddresses.Remove(address);
            
            // Check if the location is used by other employee addresses
            var otherAddressesWithSameLocation = await _context.EmployeeAddresses
                .Where(a => a.LocationId == address.LocationId && a.Id != address.Id)
                .ToListAsync();
                
            // If no other addresses use this location, delete it
            if (!otherAddressesWithSameLocation.Any() && locationToDelete != null)
            {
                _context.Locations.Remove(locationToDelete);
            }
            
            await _context.SaveChangesAsync();

            if (string.IsNullOrEmpty(StatusMessage))
            {
                StatusMessage = "Address successfully deleted.";
            }

            return RedirectToPage();
        }
    }
}
