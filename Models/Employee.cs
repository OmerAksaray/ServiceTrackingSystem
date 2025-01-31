using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceTrackingSystem.Models
{
    public class Employee : ApplicationUser
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Surname { get; set; }

        [ForeignKey("Driver")]
        public string? DriverId { get; set; }  
        public virtual Driver? Driver { get; set; }

        public List<EmployeeAddress> Addresses { get; set; } = new();
        public List<RouteAssignment> RouteAssignments { get; set; } = new();
    }
}
