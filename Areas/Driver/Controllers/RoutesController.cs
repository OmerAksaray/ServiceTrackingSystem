using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceTrackingSystem.Models;
using ServiceTrackingSystem.Services;

namespace ServiceTrackingSystem.Areas.Driver.Controllers
{
    [Area("Driver")]
    //[IgnoreAntiforgeryToken]          // ← token göndermiyorsan bırak; gönderiyorsan sil
    public class RoutesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RoutesController> _logger;
        private readonly IGeocodingService _geocoder;
        private readonly IRoutingService _routing;
        private readonly string _googleMapsApiKey = "AIzaSyAWz9m9F9fHKVNI_soWwdxWsWcyCQfukyE";

        public RoutesController(AppDbContext context,
                                ILogger<RoutesController> logger,
                                IGeocodingService geocoder,
                                IRoutingService routing)
        {
            _context = context;
            _logger = logger;
            _geocoder = geocoder;
            _routing = routing;
        }

        // GET: /Driver/Routes
        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (userId == 0) return Challenge();

            var assignmentIds = await _context.EmployeeAddresses
                                   .Where(r => r.IsActive && r.Driver.Id == userId)
                                   .Select(r => r.LocationId)
                                   .ToListAsync();

            var locations = await _context.Locations
                               .Where(l => assignmentIds.Contains(l.Id))
                               .ToListAsync();

            return View(locations);
        }

        // GET: /Driver/Routes/CombineRoutes
        public async Task<IActionResult> CombineRoutes() => await Index();

        // GET: /Driver/Routes/Navigate/5
        public async Task<IActionResult> Navigate(int id)
        {
            var location = await _context.Locations.FindAsync(id);
            
            if (location == null)
            {
                return NotFound();
            }

            if (!location.Latitude.HasValue || !location.Longitude.HasValue)
            {
                // Try to geocode the address if coordinates are missing
                var address = string.Join(", ", new[]
                { 
                    location.StreetName, 
                    location.NeighborhoodName, 
                    location.DistrictName, 
                    location.CityName, 
                    location.Country 
                }.Where(s => !string.IsNullOrWhiteSpace(s)));

                try
                {
                    var (lat, lon) = await _geocoder.ForwardAsync(address);
                    location.Latitude = lat;
                    location.Longitude = lon;
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to geocode address: {Address}", address);
                    return BadRequest("Konum koordinatları bulunamadı");
                }
            }

            ViewBag.GoogleMapsApiKey = _googleMapsApiKey;
            ViewBag.Destination = new { 
                lat = location.Latitude.Value, 
                lng = location.Longitude.Value,
                address = location.DetailedAddress ?? location.AddressLine ?? 
                        $"{location.StreetName}, {location.NeighborhoodName}, {location.DistrictName}, {location.CityName}"
            };

            return View(location);
        }

        // GET: /Driver/Routes/NavigateAll
        public async Task<IActionResult> NavigateAll()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (userId == 0) return Challenge();

            var assignmentIds = await _context.EmployeeAddresses
                                  .Where(r => r.IsActive && r.Driver.Id == userId)
                                  .Select(r => r.LocationId)
                                  .ToListAsync();

            var locations = await _context.Locations
                               .Where(l => assignmentIds.Contains(l.Id))
                               .ToListAsync();

            if (locations == null || !locations.Any())
            {
                return NotFound("Aktif lokasyon bulunamadı");
            }

            // Ensure all locations have coordinates
            foreach (var location in locations.Where(l => !l.Latitude.HasValue || !l.Longitude.HasValue))
            {
                var address = string.Join(", ", new[]
                { 
                    location.StreetName, 
                    location.NeighborhoodName, 
                    location.DistrictName, 
                    location.CityName, 
                    location.Country 
                }.Where(s => !string.IsNullOrWhiteSpace(s)));

                try
                {
                    var (lat, lon) = await _geocoder.ForwardAsync(address);
                    location.Latitude = lat;
                    location.Longitude = lon;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to geocode address: {Address}", address);
                    // Continue with other locations even if this one fails
                    continue;
                }
            }

            // Save any geocoded coordinates
            await _context.SaveChangesAsync();

            // Filter out locations without coordinates
            var validLocations = locations.Where(l => l.Latitude.HasValue && l.Longitude.HasValue).ToList();

            if (!validLocations.Any())
            {
                return BadRequest("Geçerli koordinat bulunamadı");
            }

            ViewBag.GoogleMapsApiKey = _googleMapsApiKey;
            ViewBag.Waypoints = validLocations.Select(l => new
            {
                lat = l.Latitude.Value,
                lng = l.Longitude.Value,
                address = l.DetailedAddress ?? l.AddressLine ?? 
                         $"{l.StreetName}, {l.NeighborhoodName}, {l.DistrictName}, {l.CityName}"
            }).ToList();

            return View(validLocations);
        }

        // GET: /Driver/Routes/GetLocationCoordinates/5
        [HttpGet]
        public async Task<IActionResult> GetLocationCoordinates(int id)
        {
            var location = await _context.Locations.FindAsync(id);
            
            if (location == null)
            {
                return NotFound();
            }

            if (!location.Latitude.HasValue || !location.Longitude.HasValue)
            {
                return BadRequest("Konum koordinatları bulunamadı");
            }

            return Json(new { 
                lat = location.Latitude.Value, 
                lng = location.Longitude.Value,
                address = location.DetailedAddress ?? location.AddressLine ?? 
                        $"{location.StreetName}, {location.NeighborhoodName}, {location.DistrictName}, {location.CityName}"
            });
        }

        // GET: /Driver/Routes/GetAllLocationCoordinates
        [HttpGet]
        public async Task<IActionResult> GetAllLocationCoordinates()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (userId == 0) return Challenge();

            var assignmentIds = await _context.EmployeeAddresses
                                  .Where(r => r.IsActive && r.Driver.Id == userId)
                                  .Select(r => r.LocationId)
                                  .ToListAsync();

            var locations = await _context.Locations
                               .Where(l => assignmentIds.Contains(l.Id) && l.Latitude.HasValue && l.Longitude.HasValue)
                               .ToListAsync();

            var result = locations.Select(l => new
            {
                id = l.Id,
                lat = l.Latitude.Value,
                lng = l.Longitude.Value,
                address = l.DetailedAddress ?? l.AddressLine ?? 
                         $"{l.StreetName}, {l.NeighborhoodName}, {l.DistrictName}, {l.CityName}"
            }).ToList();

            return Json(result);
        }

        //// POST: /Driver/Routes/BuildRoute
        //[HttpPost]
        //public async Task<IActionResult> BuildRoute([FromBody] int[] locationIds)
        //{
        //    _logger.LogInformation("BuildRoute called with {Count} IDs", locationIds?.Length ?? 0);

        //    if (locationIds is null || locationIds.Length < 2)
        //        return BadRequest(new { error = "En az iki nokta seçin" });

        //    var locations = await _context.Locations
        //                       .Where(l => locationIds.Contains(l.Id))
        //                       .ToListAsync();

        //    if (locations.Count != locationIds.Length)
        //    {
        //        var missing = locationIds.Except(locations.Select(l => l.Id));
        //        return BadRequest(new { error = "Bazı konumlar bulunamadı", missing });
        //    }

        //    locations = locations
        //                .OrderBy(l => Array.IndexOf(locationIds, l.Id))
        //                .ToList();

        //    var coords = new List<(double lat, double lon)>();

        //    foreach (var loc in locations)
        //    {
        //        if (loc.Latitude.HasValue && loc.Longitude.HasValue)
        //        {
        //            coords.Add((loc.Latitude.Value, loc.Longitude.Value));
        //            continue;
        //        }

        //        var addr = string.Join(", ", new[]
        //        { loc.StreetName, loc.NeighborhoodName, loc.DistrictName, loc.CityName, loc.Country }
        //        .Where(s => !string.IsNullOrWhiteSpace(s)));

        //        var (lat, lon) = await _geocoder.ForwardAsync(addr);

        //        loc.Latitude = lat;
        //        loc.Longitude = lon;
        //        coords.Add((lat, lon));
        //    }

        //    await _context.SaveChangesAsync();

        //    if (coords.Count < 2)
        //        return BadRequest(new { error = "Geçerli koordinat bulunamadı" });



        //    https://maps.googleapis.com/maps/api/directions/json

        //}
    }
}
