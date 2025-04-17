using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
using Microsoft.AspNetCore.Antiforgery;

namespace ServiceTrackingSystem.Areas.Employee.Pages.EmployeePages
{
    public class AddressesModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<AddressesModel> _logger;

        public AddressesModel(UserManager<ApplicationUser> userManager, AppDbContext context, IHttpClientFactory clientFactory, ILogger<AddressesModel> logger)
        {
            _userManager = userManager;
            _context = context;
            _clientFactory = clientFactory;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Display(Name = "Detailed Address")]
            public string? Address { get; set; }

            [Display(Name = "Active Address")]
            public bool IsActive { get; set; }

            public AddressLocation Location { get; set; } = new AddressLocation();
        }

        public class AddressLocation
        {
            [Required(ErrorMessage = "City selection is required")]
            [Display(Name = "City")]
            public int CityId { get; set; } // String olarak bırakıyoruz çünkü API'den bu şekilde geliyor

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
            public string? DetailedAddress { get; set; }
        }

        public List<CityViewModel> Cities { get; set; }
        public List<DistrictViewModel> Districts { get; set; }
        public List<NeighborhoodViewModel> Neighborhoods { get; set; }
        public List<StreetViewModel> Streets { get; set; }
        public List<EmployeeAddress> EmployeeAddresses { get; set; }
        [TempData]
        public string StatusMessage { get; set; }
        public int? EditingAddressId { get; set; }

        public class CityViewModel
        {
            public int CityId { get; set; }
            public string CityName { get; set; }
        }

        public class DistrictViewModel
        {
            public string DistrictName { get; set; }
            public int CityId { get; set; }  
        }

        public class NeighborhoodViewModel
        {
            public string NeighborhoodName { get; set; }
            public int CityId { get; set; }
            public string DistrictName { get; set; }  
        }

        public class StreetViewModel
        {
            public string StreetName { get; set; }
            public int CityId { get; set; }
            public string NeighborhoodName { get; set; }  
        }

        private async Task LoadAddresses(int userId)
        {
            try
            {
                EmployeeAddresses = await _context.EmployeeAddresses
                    .Include(a => a.Location)
                    .Where(a => a.EmployeeId == userId)
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
                client.Timeout = TimeSpan.FromSeconds(30); // Set reasonable timeout
                
                // Log the API request
                _logger.LogInformation("Making request to city API: https://tradres.com.tr/api/iller");
                
                var response = await client.GetAsync("https://tradres.com.tr/api/iller");
                
                // Initialize Cities list to avoid null reference
                Cities = new List<CityViewModel>();

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"API Response received, length: {content.Length}");
                    
                    // Parse the JSON array directly instead of using City model
                    try 
                    {
                        var jsonArray = JArray.Parse(content);
                        _logger.LogInformation($"Parsed JSON array with {jsonArray.Count} cities");
                        
                        foreach (var item in jsonArray)
                        {
                            int cityId = item["ilId"].Value<int>();
                            string cityName = item["ilAdi"].Value<string>();
                            
                            _logger.LogInformation($"Parsed city: ID={cityId}, Name={cityName}");
                            
                            Cities.Add(new CityViewModel
                            {
                                CityId = cityId,
                                CityName = cityName
                            });
                        }
                        
                        _logger.LogInformation($"Successfully loaded {Cities.Count} cities");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error parsing city data: {ex.Message}");
                        StatusMessage = "Error loading city data. Please try again later.";
                    }
                }
                else
                {
                    _logger.LogError($"Error loading city information. API response code: {response.StatusCode}");
                    StatusMessage = $"Error loading city information. API response code: {response.StatusCode}";
                    
                    // Add some dummy data for testing if needed
                     
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception when loading cities");
                StatusMessage = $"Error loading city information: {ex.Message}";
                
                // Add some dummy data for testing if needed
                 
            }
        }

        [HttpGet]
        [IgnoreAntiforgeryToken]
        public async Task<JsonResult> OnGetDistrictsAsync(int cityId)
        {
            try
            {
                var client = _clientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30); // Set reasonable timeout

                // Log the API request
                _logger.LogInformation($"Making request to district API: https://tradres.com.tr/api/ilceler?ilkod={cityId}");

                var response = await client.GetAsync($"https://tradres.com.tr/api/ilceler?ilkod={cityId}");

                // Initialize Districts list to avoid null reference
                Districts = new List<DistrictViewModel>();

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"API Response received, length: {content.Length}");

                    // Parse the JSON array directly instead of using District model
                    try
                    {
                        var jsonArray = JArray.Parse(content);
                        _logger.LogInformation($"Parsed JSON array with {jsonArray.Count} districts");

                        // Get city name for the reference
                        string cityName = await GetCityNameByIdAsync(cityId);

                        foreach (var item in jsonArray)
                        {
                            string districtName = item["ilceAdi"].Value<string>();
                            // Use ilceAdi as value since that's what we need for the next API call
                            Districts.Add(new DistrictViewModel
                            {
                                DistrictName = districtName,
                                CityId = cityId
                            });
                        }

                        _logger.LogInformation($"Successfully loaded {Districts.Count} districts");
                        
                        // Return the districts as JSON
                        return new JsonResult(Districts);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error parsing district data: {ex.Message}");
                        StatusMessage = "Error loading district data. Please try again later.";
                        return new JsonResult(new { error = ex.Message });
                    }
                }
                else
                {
                    _logger.LogError($"Error loading district information. API response code: {response.StatusCode}");
                    StatusMessage = $"Error loading district information. API response code: {response.StatusCode}";

                    // Return error as JSON
                    return new JsonResult(new { error = $"Error: {response.StatusCode}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception when loading districts");
                StatusMessage = $"Error loading district information: {ex.Message}";

                // Return error as JSON
                return new JsonResult(new { error = ex.Message });
            }
        }

        [HttpGet]
        [IgnoreAntiforgeryToken]
        public async Task<JsonResult> OnGetNeighborhoodsAsync(string districtName, int cityId)
        {
            try
            {
                var client = _clientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30); // Set reasonable timeout

                // Log the API request
                _logger.LogInformation($"Making request to neighborhood API: https://tradres.com.tr/api/mahalleler?ilkod={cityId}&ilce={districtName}");

                // Properly encode the Turkish characters in the district name
                var encodedDistrictName = HttpUtility.UrlEncode(districtName);
                var response = await client.GetAsync($"https://tradres.com.tr/api/mahalleler?ilkod={cityId}&ilce={encodedDistrictName}");

                // Initialize Neighborhoods list to avoid null reference
                Neighborhoods = new List<NeighborhoodViewModel>();

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"API Response received, length: {content.Length}");

                    // Parse the JSON array directly
                    try
                    {
                        var jsonArray = JArray.Parse(content);
                        _logger.LogInformation($"Parsed JSON array with {jsonArray.Count} neighborhoods");

                        foreach (var item in jsonArray)
                        {
                            string neighborhoodName = item["mahalleAdi"].Value<string>();

                            _logger.LogInformation($"Parsed neighborhood: Name={neighborhoodName}");

                            Neighborhoods.Add(new NeighborhoodViewModel
                            {
                                NeighborhoodName = neighborhoodName,
                                CityId = cityId,
                                DistrictName = districtName
                            });
                        }

                        _logger.LogInformation($"Successfully loaded {Neighborhoods.Count} neighborhoods");
                        
                        // Return the neighborhoods as JSON
                        return new JsonResult(Neighborhoods);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error parsing neighborhood data: {ex.Message}");
                        StatusMessage = "Error loading neighborhood data. Please try again later.";
                        return new JsonResult(new { error = ex.Message });
                    }
                }
                else
                {
                    _logger.LogError($"Error loading neighborhood information. API response code: {response.StatusCode}");
                    StatusMessage = $"Error loading neighborhood information. API response code: {response.StatusCode}";

                    // Return error as JSON
                    return new JsonResult(new { error = $"Error: {response.StatusCode}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception when loading neighborhoods");
                StatusMessage = $"Error loading neighborhood information: {ex.Message}";

                // Return error as JSON
                return new JsonResult(new { error = ex.Message });
            }
        }

        [HttpGet]
        [IgnoreAntiforgeryToken]
        public async Task<JsonResult> OnGetStreetsAsync(string neighborhoodName, int cityId)
        {
            try
            {
                // Clean up neighborhood name by removing 'MAHALLESİ' if present
                string cleanNeighborhoodName = neighborhoodName;
                if (neighborhoodName.Contains("MAHALLESİ", StringComparison.OrdinalIgnoreCase))
                {
                    cleanNeighborhoodName = neighborhoodName.Replace(" MAHALLESİ", "", StringComparison.OrdinalIgnoreCase);
                    _logger.LogInformation($"Cleaned neighborhood name: {cleanNeighborhoodName} (original: {neighborhoodName})");
                }
                
                var client = _clientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30); // Set reasonable timeout

                // Log the API request with cleaned neighborhood name
                _logger.LogInformation($"Making request to street API: https://tradres.com.tr/api/sokaklar?ilkod={cityId}&mahalle={cleanNeighborhoodName}");

                // Properly encode the Turkish characters in the cleaned neighborhood name
                var encodedNeighborhoodName = HttpUtility.UrlEncode(cleanNeighborhoodName);
                var response = await client.GetAsync($"https://tradres.com.tr/api/sokaklar?ilkod={cityId}&mahalle={encodedNeighborhoodName}");

                // Initialize Streets list to avoid null reference
                Streets = new List<StreetViewModel>();

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"API Response received, length: {content.Length}");

                    // Parse the JSON array directly
                    try
                    {
                        var jsonArray = JArray.Parse(content);
                        _logger.LogInformation($"Parsed JSON array with {jsonArray.Count} streets");

                        foreach (var item in jsonArray)
                        {
                            // According to the example, use the 'sokakAdi' field from the API response
                            string streetName = item["sokakAdi"].Value<string>();

                            _logger.LogInformation($"Parsed street: Name={streetName}");

                            Streets.Add(new StreetViewModel
                            {
                                StreetName = streetName,
                                CityId = cityId,
                                NeighborhoodName = neighborhoodName // Keep original neighborhood name for UI
                            });
                        }

                        _logger.LogInformation($"Successfully loaded {Streets.Count} streets");
                        
                        // Return the streets as JSON
                        return new JsonResult(Streets);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error parsing street data: {ex.Message}");
                        StatusMessage = "Error loading street data. Please try again later.";
                        return new JsonResult(new { error = ex.Message });
                    }
                }
                else
                {
                    _logger.LogError($"Error loading street information. API response code: {response.StatusCode}");
                    StatusMessage = $"Error loading street information. API response code: {response.StatusCode}";

                    // Return error as JSON
                    return new JsonResult(new { error = $"Error: {response.StatusCode}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception when loading streets");
                StatusMessage = $"Error loading street information: {ex.Message}";

                // Return error as JSON
                return new JsonResult(new { error = ex.Message });
            }
        }


        private async Task<City> GetCityByIdAsync(int cityId)
        {
            try
            {
                var client = _clientFactory.CreateClient();
                var response = await client.GetAsync($"https://tradres.com.tr/api/iller?ilkod={cityId}");
                
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<City>(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving city with ID {cityId}");
                return null;
            }
        }

        private async Task<string> GetCityNameByIdAsync(int cityId)
        {
            try
            {
                // First try to find the city name in the already loaded cities list
                if (Cities != null && Cities.Any())
                {
                    var city = Cities.FirstOrDefault(c => c.CityId == cityId);
                    if (city != null)
                    {
                        return city.CityName;
                    }
                }

                // If not found, make an API call
                var client = _clientFactory.CreateClient();
                var response = await client.GetAsync("https://tradres.com.tr/api/iller");
                
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var cityArray = JArray.Parse(content);
                
                foreach (var item in cityArray)
                {
                    int id = item["ilId"].Value<int>();
                    if (id == cityId)
                    {
                        return item["ilAdi"].Value<string>();
                    }
                }
                
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving city name with ID {cityId}");
                return string.Empty;
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
                        Location = new AddressLocation
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
            if (!ModelState.IsValid)
            {
                await LoadCities();
                EditingAddressId = addressId;
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            try
            {
                // Fetch city data from API to ensure we have correct information
                int cityId = Input.Location.CityId;
                var client = _clientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30); // Set reasonable timeout
                
                _logger.LogInformation($"Fetching city data for ID: {cityId}");
                var cityResponse = await client.GetAsync($"https://tradres.com.tr/api/iller");
                
                if (!cityResponse.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to retrieve city data. Status code: {cityResponse.StatusCode}");
                }
                
                var cityContent = await cityResponse.Content.ReadAsStringAsync();
                var cityArray = JArray.Parse(cityContent);
                
                string cityName = string.Empty;
                foreach (var item in cityArray)
                {
                    int id = item["ilId"].Value<int>();
                    if (id == cityId)
                    {
                        cityName = item["ilAdi"].Value<string>();
                        break;
                    }
                }
                
                if (string.IsNullOrEmpty(cityName))
                {
                    throw new Exception($"City with ID {cityId} not found in API response");
                }

                // If the address will be active, first deactivate all other addresses
                // Do this in all cases upfront to ensure consistency
                if (Input.IsActive)
                {
                    _logger.LogInformation("Deactivating all existing active addresses since the new/updated address will be active");
                    var activeAddresses = await _context.EmployeeAddresses
                        .Where(ea => ea.EmployeeId == user.Id && ea.IsActive)
                        .ToListAsync();

                    foreach (var activeAddress in activeAddresses)
                    {
                        activeAddress.IsActive = false;
                        activeAddress.UpdatedDate = DateTime.UtcNow;
                    }
                    
                    // Save changes immediately to avoid potential conflicts
                    if (activeAddresses.Any())
                    {
                        await _context.SaveChangesAsync();
                        _logger.LogInformation($"Deactivated {activeAddresses.Count} previously active addresses");
                    }
                }

                if (addressId.HasValue)
                {
                    // Update existing address
                    _logger.LogInformation($"Updating existing address with ID: {addressId}");
                    var addressToUpdate = await _context.EmployeeAddresses
                        .Include(ea => ea.Location)
                        .FirstOrDefaultAsync(ea => ea.Id == addressId.Value && ea.EmployeeId == user.Id);

                    if (addressToUpdate == null)
                    {
                        StatusMessage = "Error: Address not found or you do not have permission to edit it.";
                        await LoadCities();
                        await LoadAddresses(user.Id);
                        EditingAddressId = addressId;
                        return Page();
                    }

                    // Update location information
                    addressToUpdate.Location.CityId = cityId;
                    addressToUpdate.Location.CityName = cityName;
                    addressToUpdate.Location.DistrictName = Input.Location.DistrictName;
                    addressToUpdate.Location.NeighborhoodName = Input.Location.NeighborhoodName;
                    addressToUpdate.Location.StreetName = Input.Location.StreetName;
                    addressToUpdate.Location.DetailedAddress = Input.Address; // This is now nullable
                    addressToUpdate.Location.AddressLine = Input.Address ?? string.Empty; // Provide empty string if Address is null
                    addressToUpdate.Location.UpdatedDate = DateTime.UtcNow;

                    // Set the active status (we already deactivated other addresses if needed)
                    addressToUpdate.IsActive = Input.IsActive;
                    addressToUpdate.UpdatedDate = DateTime.UtcNow;
                    

                    await _context.SaveChangesAsync();
                    StatusMessage = "Address successfully updated.";
                    return OnGetAsync(addressToUpdate.Id).Result; 
                }
                else
                {
                    // Create a new Location record
                    _logger.LogInformation("Creating new address");
                    var location = new Models.Location
                    {
                        AddressLine = Input.Address ?? string.Empty, // Provide empty string if Address is null
                        DetailedAddress = Input.Address, // This can be null
                        CityId = cityId,
                        CityName = cityName,
                        DistrictName = Input.Location.DistrictName,
                        NeighborhoodName = Input.Location.NeighborhoodName,
                        StreetName = Input.Location.StreetName,
                        City = cityName,
                        Country = "Turkey",
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow
                    };
                    _context.Locations.Add(location);
                    
                    // We need to save changes here to get the LocationId for the new record
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Created new location with ID: {location.Id}");

                    // Create the EmployeeAddress record linking the employee and the new location
                    var employeeAddress = new EmployeeAddress
                    {
                        EmployeeId = user.Id,
                        LocationId = location.Id,
                        IsActive = Input.IsActive, // This will be true if user checked the active checkbox
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow
                    };
                    _context.EmployeeAddresses.Add(employeeAddress);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Created new employee address with ID: {employeeAddress.Id}");

                    

                    StatusMessage = "New address successfully added.";

                    return OnGetAsync(employeeAddress.Id).Result;
                }

                // Redirect to addresses page after successful save
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                // Capture more detailed error information
                _logger.LogError(ex, "Error saving address");
                var errorMessage = ex.Message;
                var currentEx = ex;
                
                // Include all inner exceptions in the error message
                while (currentEx.InnerException != null)
                {
                    currentEx = currentEx.InnerException;
                    errorMessage += $" | Inner: {currentEx.Message}";
                    _logger.LogError(currentEx, "Inner exception");
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
                _logger.LogInformation($"Attempting to delete address with ID: {addressId}");
                var address = await _context.EmployeeAddresses
                    .Include(a => a.Location)
                    .FirstOrDefaultAsync(a => a.Id == addressId && a.EmployeeId == user.Id);

                if (address != null)
                {
                    _logger.LogInformation($"Found address to delete. Location ID: {address.LocationId}");
                    
                    // Check if this is the active address
                    bool wasActive = address.IsActive;
                    
                    // Remove the employee address and its location
                    _context.EmployeeAddresses.Remove(address);
                    _context.Locations.Remove(address.Location);

                    await _context.SaveChangesAsync();
                    
                    // If the deleted address was active, make another address active
                    if (wasActive)
                    {
                        _logger.LogInformation("Deleted address was active. Setting another address as active if available.");
                        var newActiveAddress = await _context.EmployeeAddresses
                            .Where(a => a.EmployeeId == user.Id)
                            .OrderByDescending(a => a.UpdatedDate)
                            .FirstOrDefaultAsync();
                        
                        if (newActiveAddress != null)
                        {
                            newActiveAddress.IsActive = true;
                            newActiveAddress.UpdatedDate = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                            _logger.LogInformation($"Set address ID {newActiveAddress.Id} as the new active address.");
                        }
                    }
                    
                    StatusMessage = "Address successfully deleted.";
                }
                else
                {
                    _logger.LogWarning($"Address with ID {addressId} not found or doesn't belong to the current user.");
                    StatusMessage = "Error! Address to delete not found or you don't have permission to delete it.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting address ID {addressId}");
                StatusMessage = $"Error deleting address: {ex.Message}";
            }
            
            // Always load data before returning to page
            await LoadCities();
            await LoadAddresses(user.Id);
            return RedirectToPage();
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
                    address.UpdatedDate = DateTime.UtcNow;
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

        /// <summary>
        /// Handler for setting an address as active via AJAX
        /// </summary>
        /// <param name="addressId">The ID of the address to set as active</param>
        /// <returns>JSON result with success status and message</returns>
        public async Task<JsonResult> OnPostSetActiveAddressAjaxAsync(int addressId)
        {
            try
            {
                _logger.LogInformation($"Setting address ID {addressId} as active via AJAX");
                
                // Get the current user
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("User not found");
                    return new JsonResult(new { success = false, message = "User not found." });
                }

                // Find the address to set as active
                var addressToSetActive = await _context.EmployeeAddresses
                    .Include(ea => ea.Location)
                    .FirstOrDefaultAsync(ea => ea.Id == addressId && ea.EmployeeId == user.Id);

                if (addressToSetActive == null)
                {
                    _logger.LogWarning($"Address ID {addressId} not found or doesn't belong to user {user.Id}");
                    return new JsonResult(new { success = false, message = "Address not found or you don't have permission to modify it." });
                }

                // Deactivate all addresses for this employee
                var activeAddresses = await _context.EmployeeAddresses
                    .Where(ea => ea.EmployeeId == user.Id && ea.IsActive)
                    .ToListAsync();

                foreach (var activeAddress in activeAddresses)
                {
                    activeAddress.IsActive = false;
                    activeAddress.UpdatedDate = DateTime.UtcNow;
                }

                // Set the selected address as active
                addressToSetActive.IsActive = true;
                addressToSetActive.UpdatedDate = DateTime.UtcNow;

                // Save the changes
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Address ID {addressId} successfully set as active via AJAX");

                return new JsonResult(new { success = true, message = "Address successfully set as active." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting address ID {addressId} as active via AJAX");
                return new JsonResult(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
    }
}
