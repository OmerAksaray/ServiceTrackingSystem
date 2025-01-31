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
        public int? DriverId { get; set; }  
        public virtual Driver? Driver { get; set; }

        public List<EmployeeAddress> Addresses { get; set; } = new();
        [ForeignKey("RouteAssignment")]
        public int? RouteAssignmentId { get; set; } 

        public virtual RouteAssignment? RouteAssignment { get; set; } 

    }
}
