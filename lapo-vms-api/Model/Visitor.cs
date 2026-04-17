namespace lapo_vms_api.Model
{
    public class Visitor
    {
        public int Id { get; set; }
        public required string FullName { get; set; }
        public required string PhoneNumber { get; set; }
        public string? PhotoPath { get; set; } = string.Empty;
        public required VisitorType VisitorType { get; set; } = VisitorType.Customer;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Visit> Visits { get; set; } = new List<Visit>();

        public VisitorIdentification? Identification { get; set; }

        public WorkerDetails? WorkerDetails { get; set; }
    }
}