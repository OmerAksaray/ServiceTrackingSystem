using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ServiceTrackingSystem.Models
{
    public class EmployeeAddress : BaseEntity
    {
        [Key]
        public int EmployeeAddressId { get; set; }

        public int EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        public int LocationId { get; set; }
        public virtual Location Location { get; set; }
        
        // Flag to indicate if this is the active address for the employee
        public bool IsActive { get; set; } = false;
    }
}
