using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;


namespace ServiceTrackingSystem.Models
{
    public class Driver : ApplicationUser
    {
        [Required]
        public string LicenseNumber { get; set; }

        public virtual RouteAssignment RouteAssignment { get; set; }  // One-to-One
    }
}
