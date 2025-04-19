using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace ServiceTrackingSystem.Models
{
    public class Location : BaseEntity
    {
        
        public string AddressLine { get; set; }
        public string City { get; set; }
        public int CityId { get; set; }
        public string CityName { get; set; }
        public string DistrictName { get; set; }
        public string NeighborhoodName { get; set; }
        public string StreetName { get; set; }
        public string? DetailedAddress { get; set; }
        public string Country { get; set; }
        
        // Koordinat alanları eklendi
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        
        // Navigation property - a location can be associated with multiple employee addresses
        public virtual ICollection<EmployeeAddress> EmployeeAddresses { get; set; } = new List<EmployeeAddress>();
    }

    public class City
    {
        [JsonPropertyName("ilId")]
        public int CityId { get; set; }

        [JsonPropertyName("ilAdi")]
        public string CityName { get; set; }
    }

    public class District
    {
        [JsonPropertyName("ilAdi")]
        public string CityName { get; set; }
        [JsonPropertyName("ilceAdi")]
        public string DistrictName { get; set; }
    }

    public class Neighborhood
    {
        public int NeighborhoodId { get; set; }

        [JsonPropertyName("ilceAdi")]
        public string DistrictName { get; set; }
        [JsonPropertyName("mahalleAdi")]
        public string NeighborhoodName { get; set; }
    }

    public class Street
    {
        public int StreetId { get; set; }

        [JsonPropertyName("mahalleAdi")]
        public string NeighborhoodName { get; set; }
        [JsonPropertyName("sokakAdi")]
        public string StreetName { get; set; }
    }
}
