using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceTrackingSystem.Models
{
    public class RouteAssignment : BaseEntity
    {
        [Required]
        [ForeignKey("Driver")]
        public int DriverId { get; set; }

        public DateTime RouteDate { get; set; }

        public virtual Driver Driver { get; set; }

        public virtual List<Employee> Employees { get; set; } = new();

    }
}
