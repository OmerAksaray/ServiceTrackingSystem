using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ServiceTrackingSystem.Models
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<EmployeeAddress> EmployeeAddresses { get; set; }
        public DbSet<RouteAssignment> RouteAssignments { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Driver ve RouteAssignment arasında 1-1 ilişki
            builder.Entity<RouteAssignment>()
                .HasOne(r => r.Driver)
                .WithOne(d => d.RouteAssignment)
                .HasForeignKey<RouteAssignment>(r => r.DriverId)
                .OnDelete(DeleteBehavior.Restrict);

            // RouteAssignment -> Employees (1-N)
            builder.Entity<RouteAssignment>()
                .HasMany(r => r.Employees)
                .WithMany(e => e.RouteAssignments);
        }
    }
}
