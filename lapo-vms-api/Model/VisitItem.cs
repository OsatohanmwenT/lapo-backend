namespace lapo_vms_api.Model

{
    public class VisitItem
    {

        public Guid Id { get; set; } = Guid.NewGuid();
        public string? SerialNumber { get; set; }
        public string? LaptopModel { get; set; }
        public Guid VisitId { get; set; }
        public Visit Visit { get; set; } = null!;
    }
}
