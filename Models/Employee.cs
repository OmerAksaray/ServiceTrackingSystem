using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceTrackingSystem.Models
{
    public class Employee : ApplicationUser
    {

        [ForeignKey("Driver")]
        public int? DriverId { get; set; }  
        public virtual Driver? Driver { get; set; }

        // Navigation property for addresses
        public virtual ICollection<EmployeeAddress> Addresses { get; set; } = new List<EmployeeAddress>();
        
        [ForeignKey("RouteAssignment")]
        public int? RouteAssignmentId { get; set; } 

        public virtual RouteAssignment? RouteAssignment { get; set; }

        public virtual ICollection<EmployeeAddress> EmployeeAddresses { get; set; } = new List<EmployeeAddress>();

    }
}
