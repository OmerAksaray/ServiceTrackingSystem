using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceTrackingSystem.Models
{
    public class EmployeeAddress : BaseEntity
    {
        public string AddressText { get; set; } // Added AddressText property for display

        public int? EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        public int? LocationId { get; set; }
        public virtual Location Location { get; set; }
        
        // Flag to indicate if this is the active address for the employee
        public bool IsActive { get; set; } = false;

        public int? DriverId { get; set; }
        public virtual Driver? Driver { get; set; }
    }
}
