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
        public DbSet<Location> Locations { get; set; }
        public DbSet<RouteAssignment> RouteAssignments { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Employee-EmployeeAddress relationship
            builder.Entity<EmployeeAddress>()
                .HasOne(ea => ea.Employee)
                .WithMany(e => e.Addresses)
                .HasForeignKey(ea => ea.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // EmployeeAddress-Location relationship
            // Each EmployeeAddress has one Location, and each Location can be associated with multiple EmployeeAddresses
            builder.Entity<EmployeeAddress>()
                .HasOne(ea => ea.Location)
                .WithMany(l => l.EmployeeAddresses)
                .HasForeignKey(ea => ea.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Driver-RouteAssignment relationship
            builder.Entity<RouteAssignment>()
                .HasOne(r => r.Driver)
                .WithOne(d => d.RouteAssignment)
                .HasForeignKey<RouteAssignment>(r => r.DriverId)
                .OnDelete(DeleteBehavior.Restrict);

            // RouteAssignment - Employee relationship (One Route can be assigned to multiple Employees, but an Employee can only have one Route)
            builder.Entity<RouteAssignment>()
                .HasMany(r => r.Employees)
                .WithOne(e => e.RouteAssignment)
                .HasForeignKey(e => e.RouteAssignmentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
