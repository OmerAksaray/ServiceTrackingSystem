using Microsoft.AspNetCore.Identity;

namespace ServiceTrackingSystem.Models

{
    public class EmployeeAddress : BaseEntity
    {
        public int EmployeeId { get; set; }

        public string Address { get; set; } 

        public bool IsActive { get; set; }
    }
}
