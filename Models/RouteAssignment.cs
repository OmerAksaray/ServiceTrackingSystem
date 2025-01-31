namespace ServiceTrackingSystem.Models
{
    public class RouteAssignment:BaseEntity
    {
        public int DriverId { get; set; }

        public int EmployeeId { get; set; }

        public DateTime RouteDate { get; set; }
    }
}
