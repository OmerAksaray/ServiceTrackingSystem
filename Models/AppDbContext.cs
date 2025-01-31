using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceTrackingSystem.Models
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int> 
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<EmployeeAddress> EmployeeAddresses { get; set; }
        public DbSet<RouteAssignment> RouteAssignments { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);


            // Driver-RouteAssignment ilişkisi
            builder.Entity<RouteAssignment>()
                .HasOne(r => r.Driver)
                .WithOne(d => d.RouteAssignment)
                .HasForeignKey<RouteAssignment>(r => r.DriverId)
                .OnDelete(DeleteBehavior.Restrict);

            // RouteAssignment - Employee ilişkisi (Bir Route birden çok Employee'ye atanabilir, ama bir Employee yalnızca bir Route'a sahip olabilir.)
            builder.Entity<RouteAssignment>()
                .HasMany(r => r.Employees)
                .WithOne(e => e.RouteAssignment)
                .HasForeignKey(e => e.RouteAssignmentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
