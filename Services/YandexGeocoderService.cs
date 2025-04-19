using System;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ServiceTrackingSystem.Services
{
    public class YandexGeocoderService : IGeocodingService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly ILogger<YandexGeocoderService> _logger;

        public YandexGeocoderService(HttpClient http, IConfiguration cfg, ILogger<YandexGeocoderService> logger = null)
        {
            _http = http;
            _logger = logger;
            _apiKey = cfg["Yandex:GeocoderKey"]
                      ?? throw new ArgumentNullException(nameof(cfg), "Yandex:GeocoderKey eksik!");
        }

        // Tuple element names match interface: (lat, lng)
        public async Task<(double lat, double lng)> ForwardAsync(string address)
        {
            try
            {
                _logger?.LogInformation("Geocoding address with Yandex: {Address}", address);
                
                var url = $"https://geocode-maps.yandex.ru/1.x/?apikey={_apiKey}"
                        + $"&format=json&geocode={Uri.EscapeDataString(address)}";

                using var stream = await _http.GetStreamAsync(url);
                using var doc = await JsonDocument.ParseAsync(stream);

                // JSON içinden pozisyonu çekiyoruz:
                var pos = doc.RootElement
                             .GetProperty("response")
                             .GetProperty("GeoObjectCollection")
                             .GetProperty("featureMember")[0]
                             .GetProperty("GeoObject")
                             .GetProperty("Point")
                             .GetProperty("pos")
                             .GetString()!;

                var parts = pos.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                // parçalardan önce lon sonra lat geliyor
                double lon = double.Parse(parts[0], CultureInfo.InvariantCulture);
                double lat = double.Parse(parts[1], CultureInfo.InvariantCulture);

                _logger?.LogInformation("Geocoded {Address} to ({Lat}, {Lng}) using Yandex", address, lat, lon);
                
                // Return with matching tuple element names
                return (lat: lat, lng: lon);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error geocoding address with Yandex: {Address}", address);
                throw;
            }
        }
        
        // Implement the required ReverseAsync method
        public async Task<string> ReverseAsync(double lat, double lng)
        {
            try
            {
                _logger?.LogInformation("Reverse geocoding coordinates with Yandex: ({Lat}, {Lng})", lat, lng);
                
                // Yandex expects longitude first, then latitude (opposite of usual convention)
                var url = $"https://geocode-maps.yandex.ru/1.x/?apikey={_apiKey}"
                        + $"&format=json&geocode={lng},{lat}";

                using var stream = await _http.GetStreamAsync(url);
                using var doc = await JsonDocument.ParseAsync(stream);

                var address = doc.RootElement
                                .GetProperty("response")
                                .GetProperty("GeoObjectCollection")
                                .GetProperty("featureMember")[0]
                                .GetProperty("GeoObject")
                                .GetProperty("metaDataProperty")
                                .GetProperty("GeocoderMetaData")
                                .GetProperty("text")
                                .GetString()!;

                _logger?.LogInformation("Reverse geocoded ({Lat}, {Lng}) to {Address} using Yandex", lat, lng, address);
                
                return address;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error reverse geocoding with Yandex: ({Lat}, {Lng})", lat, lng);
                throw;
            }
        }
    }
}
