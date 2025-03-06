using System;
using System.Collections.Generic;

namespace ServiceTrackingSystem.Models
{
    public class EmployeeAddress
    {
        public int EmployeeAddressId { get; set; }

        public int EmployeeId { get; set; }
        public virtual Employee Employee { get; set; }

        public int LocationId { get; set; }
        public virtual Location Location { get; set; }
    }
}
