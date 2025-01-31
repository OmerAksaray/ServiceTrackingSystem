namespace ServiceTrackingSystem.Models
{
    public class Driver : BaseEntity
    {

        public string Name { get; set; }

        public string Surname { get; set; }

        public string EmailAddress { get; set; }

        public string PhoneNumber { get; set; }


        public List<RouteAssignment> RouteAssigments { get; set; }
    }
}
