using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace ServiceTrackingSystem.Services
{
    public class GoogleGeocodingService : IGeocodingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GoogleGeocodingService> _logger;
        private readonly string _apiKey;

        public GoogleGeocodingService(HttpClient httpClient, ILogger<GoogleGeocodingService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = "AIzaSyAWz9m9F9fHKVNI_soWwdxWsWcyCQfukyE";
        }

        public async Task<(double lat, double lng)> ForwardAsync(string address)
        {
            try
            {
                _logger.LogInformation("Geocoding address: {Address}", address);
                
                var encodedAddress = HttpUtility.UrlEncode(address);
                var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={encodedAddress}&key={_apiKey}";
                
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<GoogleGeocodingResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result?.Status != "OK" || result.Results.Count == 0)
                {
                    _logger.LogWarning("Geocoding failed for address: {Address}. Status: {Status}", address, result?.Status);
                    throw new Exception($"Geocoding failed: {result?.Status}");
                }

                var location = result.Results[0].Geometry.Location;
                _logger.LogInformation("Geocoded {Address} to ({Lat}, {Lng})", address, location.Lat, location.Lng);
                
                return (location.Lat, location.Lng);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error geocoding address: {Address}", address);
                throw;
            }
        }

        public async Task<string> ReverseAsync(double lat, double lng)
        {
            try
            {
                _logger.LogInformation("Reverse geocoding coordinates: ({Lat}, {Lng})", lat, lng);
                
                var url = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={lat},{lng}&key={_apiKey}";
                
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<GoogleGeocodingResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result?.Status != "OK" || result.Results.Count == 0)
                {
                    _logger.LogWarning("Reverse geocoding failed for coordinates: ({Lat}, {Lng}). Status: {Status}", lat, lng, result?.Status);
                    throw new Exception($"Reverse geocoding failed: {result?.Status}");
                }

                var address = result.Results[0].FormattedAddress;
                _logger.LogInformation("Reverse geocoded ({Lat}, {Lng}) to {Address}", lat, lng, address);
                
                return address;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reverse geocoding coordinates: ({Lat}, {Lng})", lat, lng);
                throw;
            }
        }
    }

    public class GoogleGeocodingResponse
    {
        public string Status { get; set; }
        public List<GeocodingResult> Results { get; set; } = new List<GeocodingResult>();
    }

    public class GeocodingResult
    {
        public string FormattedAddress { get; set; }
        public Geometry Geometry { get; set; }
    }

    public class Geometry
    {
        public Location Location { get; set; }
    }

    public class Location
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
    }
} 