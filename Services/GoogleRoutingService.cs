using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace ServiceTrackingSystem.Services
{
    public class GoogleRoutingService : IRoutingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GoogleRoutingService> _logger;
        private readonly string _apiKey;

        public GoogleRoutingService(HttpClient httpClient, ILogger<GoogleRoutingService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = "AIzaSyAWz9m9F9fHKVNI_soWwdxWsWcyCQfukyE";
        }

        public async Task<(string polyline, List<(string instruction, int distance, int duration)> steps)> CalculateRouteAsync(
            List<(double lat, double lng)> coordinates,
            bool optimize = false)
        {
            if (coordinates == null || coordinates.Count < 2)
            {
                throw new ArgumentException("At least two coordinates are required", nameof(coordinates));
            }

            try
            {
                _logger.LogInformation("Calculating route with {Count} coordinates", coordinates.Count);

                var origin = $"{coordinates[0].lat},{coordinates[0].lng}";
                var destination = $"{coordinates[coordinates.Count - 1].lat},{coordinates[coordinates.Count - 1].lng}";

                // Build waypoints parameter if there are intermediate points
                string waypoints = string.Empty;
                if (coordinates.Count > 2)
                {
                    var waypointList = coordinates
                        .Skip(1)
                        .Take(coordinates.Count - 2)
                        .Select(c => $"{c.lat},{c.lng}");

                    waypoints = optimize ? "optimize:true|" : "";
                    waypoints += string.Join("|", waypointList);
                }

                var url = $"https://maps.googleapis.com/maps/api/directions/json" +
                          $"?origin={HttpUtility.UrlEncode(origin)}" +
                          $"&destination={HttpUtility.UrlEncode(destination)}" +
                          (!string.IsNullOrEmpty(waypoints) ? $"&waypoints={HttpUtility.UrlEncode(waypoints)}" : "") +
                          $"&mode=driving" +
                          $"&key={_apiKey}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<GoogleDirectionsResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result?.Status != "OK" || result.Routes.Count == 0)
                {
                    _logger.LogWarning("Route calculation failed. Status: {Status}", result?.Status);
                    throw new Exception($"Route calculation failed: {result?.Status}");
                }

                var route = result.Routes[0];
                var polyline = route.OverviewPolyline.Points;

                var steps = new List<(string instruction, int distance, int duration)>();
                foreach (var leg in route.Legs)
                {
                    foreach (var step in leg.Steps)
                    {
                        steps.Add((step.HtmlInstructions, step.Distance.Value, step.Duration.Value));
                    }
                }

                _logger.LogInformation("Calculated route with {StepCount} steps", steps.Count);
                return (polyline, steps);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating route");
                throw;
            }
        }
    }

    public class GoogleDirectionsResponse
    {
        public string Status { get; set; }
        public List<Route> Routes { get; set; } = new List<Route>();
    }

    public class Route
    {
        public List<Leg> Legs { get; set; } = new List<Leg>();
        public OverviewPolyline OverviewPolyline { get; set; }
    }

    public class Leg
    {
        public List<Step> Steps { get; set; } = new List<Step>();
        public ValueTextPair Distance { get; set; }
        public ValueTextPair Duration { get; set; }
    }

    public class Step
    {
        public string HtmlInstructions { get; set; }
        public ValueTextPair Distance { get; set; }
        public ValueTextPair Duration { get; set; }
    }

    public class OverviewPolyline
    {
        public string Points { get; set; }
    }

    public class ValueTextPair
    {
        public int Value { get; set; }
        public string Text { get; set; }
    }
} 