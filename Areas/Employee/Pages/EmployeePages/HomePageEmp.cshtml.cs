using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ServiceTrackingSystem.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ServiceTrackingSystem.Areas.Employee.Pages.EmployeePages
{
    public class HomePageEmpModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly HttpClient client = new HttpClient();
        private readonly ILogger<HomePageEmpModel> _logger;

        public HomePageEmpModel(AppDbContext context, UserManager<ApplicationUser> userManager, ILogger<HomePageEmpModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public EmployeeAddress EmployeeAddress { get; set; } = new();

        public List<City> Cities { get; set; }

        public List<EmployeeAddress> EmployeeAddresses { get; set; }

        public class InputModel
        {
            [Required]
            public string Address { get; set; }

            [Required]
            public Location Location { get; set; }

            public bool IsActive { get; set; } = true;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync("https://tradres.com.tr/api/iller");
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                Cities = JsonSerializer.Deserialize<List<City>>(responseBody);

                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    // Get addresses directly using AsNoTracking for better performance
                    EmployeeAddresses = await _context.EmployeeAddresses
                        .AsNoTracking()
                        .Where(a => a.EmployeeId == user.Id)
                        .ToListAsync();
                }

                return Page();
            }
            catch (HttpRequestException e)
            {
                _logger.LogError("Request error: {ErrorMessage}", e.Message);
                return Page();
            }
        }

        // Method to fetch a specific city by ilkod
        public async Task<City> GetCityByIdAsync(int cityId)
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync($"https://tradres.com.tr/api/iller?ilkod={cityId}");
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<City>(responseBody);
            }
            catch (HttpRequestException e)
            {
                _logger.LogError("Request error when fetching city: {ErrorMessage}", e.Message);
                return null;
            }
        }

        public async Task<IActionResult> OnGetDistrictsAsync(string cityId)
        {
            try
            {
                var response = await client.GetAsync($"https://tradres.com.tr/api/ilceler?ilkod={cityId}");
                response.EnsureSuccessStatusCode();
                string districtsJson = await response.Content.ReadAsStringAsync();
                var districts = JsonSerializer.Deserialize<List<District>>(districtsJson);
                return new JsonResult(districts);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error fetching districts: {ErrorMessage}", ex.Message);
                return new JsonResult(new List<District>());
            }
        }

        public async Task<IActionResult> OnGetNeighborhoodsAsync(string cityId, string district)
        {
            try
            {
                var encodedDistrict = Uri.EscapeDataString(district);
                var response = await client.GetAsync($"https://tradres.com.tr/api/mahalleler?ilkod={cityId}&ilce={encodedDistrict}");
                response.EnsureSuccessStatusCode();
                
                string neighborhoodsJson = await response.Content.ReadAsStringAsync();
                var neighborhoods = JsonSerializer.Deserialize<List<Neighborhood>>(neighborhoodsJson);
                return new JsonResult(neighborhoods);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error fetching neighborhoods: {ErrorMessage}", ex.Message);
                return new JsonResult(new List<Neighborhood>());
            }
        }

        public async Task<IActionResult> OnGetStreetsAsync(string cityId, string neighborhood)
        {
            try
            {
                var encodedNeighborhood = Uri.EscapeDataString(neighborhood);
                var response = await client.GetAsync($"https://tradres.com.tr/api/sokaklar?ilkod={cityId}&mahalle={encodedNeighborhood}");
                response.EnsureSuccessStatusCode();
                
                string streetsJson = await response.Content.ReadAsStringAsync();
                var streets = JsonSerializer.Deserialize<List<Street>>(streetsJson);
                return new JsonResult(streets);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error fetching streets: {ErrorMessage}", ex.Message);
                return new JsonResult(new List<Street>());
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Fetch city from API to ensure we have the correct data
            try
            {
                int cityId = Input.Location.CityId;
                var cityResponse = await client.GetAsync($"https://tradres.com.tr/api/iller?ilkod={cityId}");
                cityResponse.EnsureSuccessStatusCode();
                
                var cityJsonContent = await cityResponse.Content.ReadAsStringAsync();
                var city = JsonSerializer.Deserialize<City>(cityJsonContent);
                
                // Create new address directly with user ID
                var newAddress = new EmployeeAddress
                {
                    EmployeeId = user.Id,
                    Location = new Location
                    {
                        CityId = city.CityId,
                        CityName = city.CityName,
                        DistrictName = Input.Location.DistrictName,
                        NeighborhoodName = Input.Location.NeighborhoodName,
                        StreetName = Input.Location.StreetName,
                        DetailedAddress = Input.Address
                    },
                    IsActive = Input.IsActive
                };

                _context.EmployeeAddresses.Add(newAddress);
                await _context.SaveChangesAsync();

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error creating address: {ErrorMessage}", ex.Message);
                ModelState.AddModelError(string.Empty, $"Error creating address: {ex.Message}");
                await OnGetAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostSetActiveAsync(int addressId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Get all addresses for this user
            var addresses = await _context.EmployeeAddresses
                .Where(a => a.EmployeeId == user.Id)
                .ToListAsync();

            // Set the selected address as active and others as inactive
            foreach (var address in addresses)
            {
                address.IsActive = address.Id == addressId;
            }

            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int addressId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Find the address for this user
            var address = await _context.EmployeeAddresses
                .FirstOrDefaultAsync(a => a.Id == addressId && a.EmployeeId == user.Id);

            if (address == null)
            {
                return NotFound();
            }

            _context.EmployeeAddresses.Remove(address);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}
