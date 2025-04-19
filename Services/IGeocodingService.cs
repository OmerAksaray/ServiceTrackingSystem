using System.Threading.Tasks;

namespace ServiceTrackingSystem.Services
{
    /// <summary>
    /// Interface for geocoding services to convert addresses into coordinates
    /// </summary>
    public interface IGeocodingService
    {
        /// <summary>
        /// Forward geocoding: Convert an address to coordinates
        /// </summary>
        /// <param name="address">The address to geocode</param>
        /// <returns>Latitude and longitude as a tuple</returns>
        Task<(double lat, double lng)> ForwardAsync(string address);
        
        /// <summary>
        /// Reverse geocoding: Convert coordinates to an address
        /// </summary>
        /// <param name="lat">Latitude</param>
        /// <param name="lng">Longitude</param>
        /// <returns>Address as a string</returns>
        Task<string> ReverseAsync(double lat, double lng);
    }
} 