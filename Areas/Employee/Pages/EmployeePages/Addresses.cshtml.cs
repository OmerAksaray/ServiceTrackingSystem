using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using ServiceTrackingSystem.Models;

namespace ServiceTrackingSystem.Areas.Employee.Pages.EmployeePages
{
    public class AddressesModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _clientFactory;

        public AddressesModel(UserManager<ApplicationUser> userManager, AppDbContext context, IHttpClientFactory clientFactory)
        {
            _userManager = userManager;
            _context = context;
            _clientFactory = clientFactory;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Address is required")]
            [Display(Name = "Detailed Address")]
            public string Address { get; set; }

            [Display(Name = "Active Address")]
            public bool IsActive { get; set; }

            public Location Location { get; set; } = new Location();
        }

        public class Location
        {
            [Required(ErrorMessage = "City selection is required")]
            [Display(Name = "City")]
            public string CityId { get; set; } // String olarak bırakıyoruz çünkü API'den bu şekilde geliyor

            public string CityName { get; set; }

            [Required(ErrorMessage = "District selection is required")]
            [Display(Name = "District")]
            public string DistrictName { get; set; }

            [Required(ErrorMessage = "Neighborhood selection is required")]
            [Display(Name = "Neighborhood")]
            public string NeighborhoodName { get; set; }

            [Required(ErrorMessage = "Street selection is required")]
            [Display(Name = "Street")]
            public string StreetName { get; set; }

            [Display(Name = "Detailed Address")]
            public string DetailedAddress { get; set; }
        }

        public List<CityViewModel> Cities { get; set; }
        public List<EmployeeAddress> EmployeeAddresses { get; set; }
        [TempData]
        public string StatusMessage { get; set; }
        public int? EditingAddressId { get; set; }

        public class CityViewModel
        {
            public string CityId { get; set; }
            public string CityName { get; set; }
        }

        private async Task LoadAddresses(int userId)
        {
            try
            {
                EmployeeAddresses = await _context.EmployeeAddresses
                    .Include(a => a.Location)
                    .Where(a => a.EmployeeId == userId)
                    .OrderByDescending(a => a.IsActive)
                    .ThenByDescending(a => a.UpdatedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading addresses: {ex.Message}";
            }
        }

        private async Task LoadCities()
        {
            try
            {
                var client = _clientFactory.CreateClient();
                var response = await client.GetAsync("https://tradres.com.tr/api/iller");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var cities = JsonConvert.DeserializeObject<List<dynamic>>(content);
                    Cities = new List<CityViewModel>();
                    
                    foreach (var city in cities)
                    {
                        Cities.Add(new CityViewModel
                        {
                            CityId = city.plaka?.ToString() ?? city.ilKod?.ToString() ?? city.ToString(),
                            CityName = city.sehir?.ToString() ?? city.ilAdi?.ToString() ?? city.ToString()
                        });
                    }
                }
                else
                {
                    // API error, create an empty list
                    Cities = new List<CityViewModel>();
                    StatusMessage = $"Error loading city information. API response code: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                Cities = new List<CityViewModel>();
                StatusMessage = $"Error loading city information: {ex.Message}";
            }
        }

        public async Task<IActionResult> OnGetAsync(int? addressId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            await LoadCities();
            await LoadAddresses(user.Id);

            // Edit mode
            if (addressId.HasValue)
            {
                EditingAddressId = addressId;
                
                var addressToEdit = EmployeeAddresses.FirstOrDefault(a => a.Id == addressId.Value);
                if (addressToEdit != null)
                {
                    Input = new InputModel
                    {
                        Address = addressToEdit.Location.DetailedAddress,
                        IsActive = addressToEdit.IsActive,
                        Location = new Location
                        {
                            CityId = addressToEdit.Location.CityId,
                            CityName = addressToEdit.Location.CityName,
                            DistrictName = addressToEdit.Location.DistrictName,
                            NeighborhoodName = addressToEdit.Location.NeighborhoodName,
                            StreetName = addressToEdit.Location.StreetName,
                            DetailedAddress = addressToEdit.Location.DetailedAddress
                        }
                    };
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSaveAddressAsync(int? addressId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            // Debug: Log all validation errors
            var errorList = new List<string>();
            foreach (var state in ModelState)
            {
                if (state.Value.Errors.Any())
                {
                    errorList.Add($"{state.Key}: {string.Join(", ", state.Value.Errors.Select(e => e.ErrorMessage))}");
                }
            }
            
            if (errorList.Any())
            {
                StatusMessage = $"Validation errors: {string.Join(" | ", errorList)}";
            }
            
            // DetailedAddress validasyonunu manuel olarak kaldıralım
            if (ModelState.ContainsKey("Input.Location.DetailedAddress"))
            {
                ModelState.Remove("Input.Location.DetailedAddress");
            }

            if (!ModelState.IsValid)
            {
                EditingAddressId = addressId;
                // Ensure Cities are always loaded even on validation failures
                await LoadCities();
                await LoadAddresses(user.Id);
                return Page();
            }

            try
            {
                // Şimdi burada Input.Address değerini Input.Location.DetailedAddress'e atalım
                Input.Location.DetailedAddress = Input.Address;

                // Check if we're updating or adding a new address
                if (addressId.HasValue)
                {
                    // Find and update existing address
                    var addressToUpdate = await _context.EmployeeAddresses
                        .Include(ea => ea.Location)
                        .FirstOrDefaultAsync(ea => ea.Id == addressId.Value && ea.EmployeeId == user.Id);

                    if (addressToUpdate == null)
                    {
                        StatusMessage = "Error! Address to update not found!";
                        await LoadCities();
                        await LoadAddresses(user.Id);
                        EditingAddressId = addressId;
                        return Page();
                    }

                    // Update location information
                    addressToUpdate.Location.CityId = Input.Location.CityId;
                    addressToUpdate.Location.CityName = Input.Location.CityName;
                    addressToUpdate.Location.DistrictName = Input.Location.DistrictName;
                    addressToUpdate.Location.NeighborhoodName = Input.Location.NeighborhoodName;
                    addressToUpdate.Location.StreetName = Input.Location.StreetName;
                    addressToUpdate.Location.DetailedAddress = Input.Location.DetailedAddress;

                    // If marked as active, deactivate other addresses
                    if (Input.IsActive && !addressToUpdate.IsActive)
                    {
                        var activeAddresses = await _context.EmployeeAddresses
                            .Where(ea => ea.EmployeeId == user.Id && ea.IsActive && ea.Id != addressId.Value)
                            .ToListAsync();

                        foreach (var activeAddress in activeAddresses)
                        {
                            activeAddress.IsActive = false;
                        }
                    }

                    addressToUpdate.IsActive = Input.IsActive;
                    addressToUpdate.UpdatedDate = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    StatusMessage = "Address successfully updated.";
                }
                else
                {
                    // Create a new Location record
                    var location = new Location
                    {
                        AddressLine = Input.Address,
                        City = Input.Location.CityName,
                        Country = "Turkey"
                    };
                    _context.Locations.Add(location);
                    await _context.SaveChangesAsync();

                    // Obtain current employee id (this should come from the logged in user context)
                    int currentEmployeeId = user.Id; // Replace with actual logic

                    // Create the EmployeeAddress record linking the employee and the new location
                    var employeeAddress = new EmployeeAddress
                    {
                        EmployeeId = currentEmployeeId,
                        LocationId = location.LocationId
                    };
                    _context.EmployeeAddresses.Add(employeeAddress);
                    await _context.SaveChangesAsync();

                    StatusMessage = "New address successfully added.";
                }

                // Redirect to address management page after successful save
                return RedirectToPage("./AddressManager");
            }
            catch (Exception ex)
            {
                // Daha detaylı hata mesajını gösterebilmek için exception yakalama kısmını düzenleyelim
                var errorMessage = ex.Message;
                var currentEx = ex;
                
                // Tüm iç hataları da ekleyelim
                while (currentEx.InnerException != null)
                {
                    currentEx = currentEx.InnerException;
                    errorMessage += $" | Inner: {currentEx.Message}";
                }
                
                StatusMessage = $"Error saving address: {errorMessage}";
                // Ensure we load cities and addresses in case of error
                EditingAddressId = addressId;
            }

            // In case of error, return to the current page
            await LoadCities();
            await LoadAddresses(user.Id);
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAddressAsync(int addressId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            try
            {
                var address = await _context.EmployeeAddresses
                    .Include(a => a.Location)
                    .FirstOrDefaultAsync(a => a.Id == addressId && a.EmployeeId == user.Id);

                if (address != null)
                {
                    _context.Locations.Remove(address.Location);
                    _context.EmployeeAddresses.Remove(address);

                    await _context.SaveChangesAsync();
                    StatusMessage = "Address successfully deleted.";
                }
                else
                {
                    StatusMessage = "Error! Address to delete not found!";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error deleting address: {ex.Message}";
            }
            
            // Always load data before returning to page
            await LoadCities();
            await LoadAddresses(user?.Id ?? 0);
            return Page();
        }

        public async Task<IActionResult> OnPostSetActiveAddressAsync(int addressId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            try
            {
                // First, make all addresses inactive
                var activeAddresses = await _context.EmployeeAddresses
                    .Where(a => a.EmployeeId == user.Id && a.IsActive)
                    .ToListAsync();

                foreach (var address in activeAddresses)
                {
                    address.IsActive = false;
                }

                // Set the selected address as active
                var addressToActivate = await _context.EmployeeAddresses
                    .FirstOrDefaultAsync(a => a.Id == addressId && a.EmployeeId == user.Id);

                if (addressToActivate != null)
                {
                    addressToActivate.IsActive = true;
                    addressToActivate.UpdatedDate = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    StatusMessage = "Address successfully set as active.";
                }
                else
                {
                    StatusMessage = "Error! Address to activate not found!";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error setting address as active: {ex.Message}";
            }

            // Always load data before returning to page
            await LoadCities();
            await LoadAddresses(user?.Id ?? 0);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAddressAsync(int addressId)
        {
            // Redirect to edit page with addressId
            return RedirectToPage(new { addressId });
        }

        public async Task<JsonResult> OnGetDistrictsAsync(string cityId)
        {
            try
            {
                Console.WriteLine($"Received cityId parameter: {cityId}");
                
                // Extract city code (may be JSON or direct code)
                string ilKod = cityId;
                try {
                    // Try JSON parsing
                    dynamic cityObject = JsonConvert.DeserializeObject<dynamic>(cityId);
                    ilKod = cityObject.ilId?.ToString() ?? cityObject.plaka?.ToString() ?? cityId;
                    Console.WriteLine($"City code extracted from JSON: {ilKod}");
                }
                catch (Exception ex) {
                    // Assume direct city code
                    Console.WriteLine($"City code parsing error (not a problem): {ex.Message}");
                }
                
                var client = _clientFactory.CreateClient();
                // Districts API expects ilKod parameter
                var url = $"https://tradres.com.tr/api/ilceler?ilkod={ilKod}";
                Console.WriteLine($"API call: {url}");
                
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API response: {content}");
                    
                    // Inspect JSON structure for received data
                    var districts = JsonConvert.DeserializeObject<dynamic>(content);
                    
                    // Log JSON structure in detail
                    if (districts != null)
                    {
                        Console.WriteLine($"Deserialized data type: {districts.GetType().Name}");
                        Console.WriteLine($"Number of data: {districts.Count}");
                        
                        // Inspect first item (if exists)
                        if (districts.Count > 0)
                        {
                            var firstItem = districts[0];
                            Console.WriteLine($"First item: {JsonConvert.SerializeObject(firstItem)}");
                            Console.WriteLine($"Existing properties:");
                            // Fix JObject conversion error
                            try {
                                foreach (JProperty prop in ((JObject)firstItem).Properties())
                                {
                                    Console.WriteLine($"  - {prop.Name}: {prop.Value}");
                                }
                            } catch (Exception ex) {
                                Console.WriteLine($"Properties reading error: {ex.Message}");
                            }
                        }
                        
                        // Prepare formatted data for frontend
                        var formattedDistricts = new List<object>();
                        foreach (var district in districts)
                        {
                            // Add district names to formatted data in frontend format
                            // Check all fields even if district name is missing
                            string ilceAdi = null;
                            try
                            {
                                ilceAdi = district.ilceAdi?.ToString() ?? 
                                        district.ad?.ToString() ?? 
                                        district.adi?.ToString() ??
                                        district.name?.ToString() ??
                                        district.ToString();
                            }
                            catch
                            {
                                // If this field is missing, log all fields
                                try {
                                    Console.WriteLine($"All fields: {JsonConvert.SerializeObject(district)}");
                                } catch {}
                            }
                            
                            formattedDistricts.Add(new { ilceAdi = ilceAdi });
                        }
                        
                        Console.WriteLine($"Data to be sent to frontend: {JsonConvert.SerializeObject(formattedDistricts)}");
                        return new JsonResult(formattedDistricts);
                    }
                    
                    return new JsonResult(districts);
                }
                else
                {
                    Console.WriteLine($"API error: {response.StatusCode}");
                    return new JsonResult(new { error = $"API response code: {response.StatusCode}" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General error: {ex.Message}");
                return new JsonResult(new { error = ex.Message });
            }
        }

        public async Task<JsonResult> OnGetNeighborhoodsAsync(string cityId, string district)
        {
            try
            {
                Console.WriteLine($"Received parameters: cityId={cityId}, district={district}");
                
                // Extract city code (may be JSON or direct code)
                string ilKod = cityId;
                try {
                    // Try JSON parsing
                    dynamic cityObject = JsonConvert.DeserializeObject<dynamic>(cityId);
                    ilKod = cityObject.ilId?.ToString() ?? cityObject.plaka?.ToString() ?? cityId;
                    Console.WriteLine($"City code extracted from JSON: {ilKod}");
                }
                catch (Exception ex) {
                    // Assume direct city code
                    Console.WriteLine($"City code parsing error (not a problem): {ex.Message}");
                }
                
                // Convert district name to URL encoded format
                string encodedDistrict = Uri.EscapeDataString(district);
                Console.WriteLine($"Encoded district name: {encodedDistrict}");
                
                var client = _clientFactory.CreateClient();
                // Neighborhoods API expects ilKod and ilçe adı parameters
                // In this format: https://tradres.com.tr/api/mahalleler?ilkod=16&ilce=ALTINŞEHİR
                var url = $"https://tradres.com.tr/api/mahalleler?ilkod={ilKod}&ilce={encodedDistrict}";
                Console.WriteLine($"API call: {url}");
                
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API response: {content}");
                    
                    // Inspect JSON structure for received data
                    var neighborhoods = JsonConvert.DeserializeObject<dynamic>(content);
                    
                    // Log JSON structure in detail
                    if (neighborhoods != null)
                    {
                        Console.WriteLine($"Deserialized data type: {neighborhoods.GetType().Name}");
                        Console.WriteLine($"Number of data: {neighborhoods.Count}");
                        
                        // Inspect first item (if exists)
                        if (neighborhoods.Count > 0)
                        {
                            var firstItem = neighborhoods[0];
                            Console.WriteLine($"First item: {JsonConvert.SerializeObject(firstItem)}");
                            Console.WriteLine($"Existing properties:");
                            // Fix JObject conversion error
                            try {
                                foreach (JProperty prop in ((JObject)firstItem).Properties())
                                {
                                    Console.WriteLine($"  - {prop.Name}: {prop.Value}");
                                }
                            } catch (Exception ex) {
                                Console.WriteLine($"Properties reading error: {ex.Message}");
                            }
                        }
                        
                        // Prepare formatted data for frontend
                        var formattedNeighborhoods = new List<object>();
                        foreach (var neighborhood in neighborhoods)
                        {
                            // Add neighborhood names to formatted data in frontend format
                            string mahalleAdi = null;
                            try
                            {
                                mahalleAdi = neighborhood.mahalleAdi?.ToString() ?? 
                                        neighborhood.ad?.ToString() ?? 
                                        neighborhood.adi?.ToString() ??
                                        neighborhood.name?.ToString() ??
                                        neighborhood.ToString();
                            }
                            catch
                            {
                                // If this field is missing, log all fields
                                try {
                                    Console.WriteLine($"All fields: {JsonConvert.SerializeObject(neighborhood)}");
                                } catch {}
                            }
                            
                            formattedNeighborhoods.Add(new { mahalleAdi = mahalleAdi });
                        }
                        
                        Console.WriteLine($"Data to be sent to frontend: {JsonConvert.SerializeObject(formattedNeighborhoods)}");
                        return new JsonResult(formattedNeighborhoods);
                    }
                    
                    return new JsonResult(neighborhoods);
                }
                else
                {
                    Console.WriteLine($"API error: {response.StatusCode}");
                    return new JsonResult(new { error = $"API response code: {response.StatusCode}" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General error: {ex.Message}");
                return new JsonResult(new { error = ex.Message });
            }
        }

        public async Task<JsonResult> OnGetStreetsAsync(string cityId, string neighborhood)
        {
            try
            {
                Console.WriteLine($"Received parameters: cityId={cityId}, neighborhood={neighborhood}");
                // Extract city code (may be JSON or direct code)
                string ilKod = cityId;
                try {
                    // Try JSON parsing
                    dynamic cityObject = JsonConvert.DeserializeObject<dynamic>(cityId);
                    ilKod = cityObject.ilId?.ToString() ?? cityObject.plaka?.ToString() ?? cityId;
                    Console.WriteLine($"City code extracted from JSON: {ilKod}");
                }
                catch (Exception ex) {
                    // Assume direct city code
                    Console.WriteLine($"City code parsing error (not a problem): {ex.Message}");
                }
                
                // Convert neighborhood name to URL encoded format
                // Neighborhood name format may need to be corrected
                string processedNeighborhood = neighborhood;
                
                // Try different neighborhood name formats that the API might accept
                // Remove suffixes like 'MAHALLESİ' or 'MAHALLESI'
                if (processedNeighborhood.EndsWith(" MAHALLESİ", StringComparison.OrdinalIgnoreCase)) {
                    processedNeighborhood = processedNeighborhood.Substring(0, processedNeighborhood.Length - " MAHALLESİ".Length).Trim();
                    Console.WriteLine($"Removed 'MAHALLESİ', new value: {processedNeighborhood}");
                } else if (processedNeighborhood.EndsWith(" MAHALLESI", StringComparison.OrdinalIgnoreCase)) {
                    processedNeighborhood = processedNeighborhood.Substring(0, processedNeighborhood.Length - " MAHALLESI".Length).Trim();
                    Console.WriteLine($"Removed 'MAHALLESI', new value: {processedNeighborhood}");
                } else if (processedNeighborhood.EndsWith(" MAH", StringComparison.OrdinalIgnoreCase)) {
                    processedNeighborhood = processedNeighborhood.Substring(0, processedNeighborhood.Length - " MAH".Length).Trim();
                    Console.WriteLine($"Removed 'MAH', new value: {processedNeighborhood}");
                } else if (processedNeighborhood.EndsWith(" M", StringComparison.OrdinalIgnoreCase)) {
                    processedNeighborhood = processedNeighborhood.Substring(0, processedNeighborhood.Length - " M".Length).Trim();
                    Console.WriteLine($"Removed 'M', new value: {processedNeighborhood}");
                }
                
                // Convert to uppercase (for APIs that expect uppercase)
                string upperNeighborhood = processedNeighborhood.ToUpper();
                
                // URL encode while preserving Turkish characters
                string encodedNeighborhood = Uri.EscapeDataString(processedNeighborhood);
                string encodedUpperNeighborhood = Uri.EscapeDataString(upperNeighborhood);
                Console.WriteLine($"Processed neighborhood name: {processedNeighborhood}");
                Console.WriteLine($"Uppercase neighborhood name: {upperNeighborhood}");
                Console.WriteLine($"Encoded neighborhood name: {encodedNeighborhood}");
                Console.WriteLine($"Encoded uppercase neighborhood name: {encodedUpperNeighborhood}");
                
                // Try three different URL formats
                string[] urlFormats = new string[] {
                    $"https://tradres.com.tr/api/sokaklar?ilkod={ilKod}&mahalle={encodedNeighborhood}",
                    $"https://tradres.com.tr/api/sokaklar?ilkod={ilKod}&mahalle={encodedUpperNeighborhood}",
                    $"https://tradres.com.tr/api/sokaklar?ilkod={ilKod}&mahalle={Uri.EscapeDataString(neighborhood)}"
                };


                var client = _clientFactory.CreateClient();
                
                // Try each format
                foreach (var url in urlFormats)
                {
                    Console.WriteLine($"Trying: {url}");
                    var response = await client.GetAsync(url);
                    Console.WriteLine($"Response status: {response.StatusCode}");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Successful API response: {content.Substring(0, Math.Min(content.Length, 500))}...");
                        
                        // Inspect JSON structure for received data
                        var streets = JsonConvert.DeserializeObject<dynamic>(content);
                        
                        // Log JSON structure in detail
                        if (streets != null)
                        {
                            Console.WriteLine($"Deserialized data type: {streets.GetType().Name}");
                            Console.WriteLine($"Number of data: {streets.Count}");
                            
                            // Inspect first item (if exists)
                            if (streets.Count > 0)
                            {
                                var firstItem = streets[0];
                                Console.WriteLine($"First item: {JsonConvert.SerializeObject(firstItem)}");
                                Console.WriteLine($"Existing properties:");
                                // Fix JObject conversion error
                                try {
                                    foreach (JProperty prop in ((JObject)firstItem).Properties())
                                    {
                                        Console.WriteLine($"  - {prop.Name}: {prop.Value}");
                                    }
                                } catch (Exception ex) {
                                    Console.WriteLine($"Properties reading error: {ex.Message}");
                                }
                            }
                            
                            // Prepare formatted data for frontend
                            var formattedStreets = new List<object>();
                            foreach (var street in streets)
                            {
                                // Add street names to formatted data in frontend format
                                string sokakAdi = null;
                                try
                                {
                                    sokakAdi = street.sokakAdi?.ToString();
                                    Console.WriteLine($"Found street name: {sokakAdi}");
                                }
                                catch (Exception ex)
                                {
                                    // If this field is missing, log all fields and try alternative fields
                                    Console.WriteLine($"sokakAdi field not found: {ex.Message}");
                                    try {
                                        Console.WriteLine($"All fields: {JsonConvert.SerializeObject(street)}");
                                        // Try alternative fields
                                        sokakAdi = street.ad?.ToString() ?? 
                                                street.adi?.ToString() ??
                                                street.name?.ToString() ??
                                                street.ToString();
                                    } catch (Exception propEx) {
                                        Console.WriteLine($"All fields reading error: {propEx.Message}");
                                    }
                                }
                                
                                // Log warning if sokakAdi is null
                                if (string.IsNullOrEmpty(sokakAdi)) {
                                    Console.WriteLine("WARNING: sokakAdi value not found!");
                                }
                                
                                formattedStreets.Add(new { sokakAdi = sokakAdi });
                            }
                            
                            Console.WriteLine($"Data to be sent to frontend: {JsonConvert.SerializeObject(formattedStreets)}");
                            return new JsonResult(formattedStreets);
                        }
                        
                        return new JsonResult(streets);
                    }
                    // If this format fails, try the next one
                }

                // If all formats fail
                Console.WriteLine("All API formats failed.");
                return new JsonResult(new { error = "API response code: BadRequest - No format succeeded" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General error: {ex.Message}");
                return new JsonResult(new { error = ex.Message });
            }
        }
    }
}
