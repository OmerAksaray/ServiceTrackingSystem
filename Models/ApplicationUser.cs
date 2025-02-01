using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceTrackingSystem.Models
{
    public class ApplicationUser : IdentityUser<int> 
    {
        [NotMapped] 
        public int EmployeeId => Id;

        public string UserType {  get; set; }
    }
}
