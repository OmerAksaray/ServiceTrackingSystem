using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceTrackingSystem.Services
{
    /// <summary>
    /// Interface for routing services that build navigation routes between coordinates
    /// </summary>
    public interface IRoutingService
    {
        /// <summary>
        /// Calculate a route between multiple points
        /// </summary>
        /// <param name="coordinates">List of coordinates (latitude, longitude)</param>
        /// <param name="optimize">Whether to optimize the waypoint order</param>
        /// <returns>Route information including polyline and steps</returns>
        Task<(string polyline, List<(string instruction, int distance, int duration)> steps)> CalculateRouteAsync(
            List<(double lat, double lng)> coordinates, 
            bool optimize = false);
    }
} 