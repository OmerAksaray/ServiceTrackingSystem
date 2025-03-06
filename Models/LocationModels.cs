using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace ServiceTrackingSystem.Models
{
    public class Location : BaseEntity
    {
        [Key]
        public int LocationId { get; set; }
        public string AddressLine { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        
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
        public int DistrictId { get; set; }
        [JsonPropertyName("ilAdi")]
        public string CityName { get; set; }
        [JsonPropertyName("IlceAdi")]
        public string DistrictName { get; set; }
    }

    public class Neighborhood
    {
        public int NeighborhoodId { get; set; }

        [JsonPropertyName("IlceAdi")]
        public string DistrictName { get; set; }
        [JsonPropertyName("MahalleAdi")]
        public string NeighborhoodName { get; set; }
    }

    public class Street
    {
        public int StreetId { get; set; }

        [JsonPropertyName("MahalleAdi")]
        public string NeighborhoodName { get; set; }
        [JsonPropertyName("SokakAdi")]
        public string StreetName { get; set; }
    }
}
