namespace lapo_vms_api.Model

{
    public class VisitItem
    {

        public int Id { get; set; }
        public string? SerialNumber { get; set; }
        public string? LaptopModel { get; set; }
        public int VisitId { get; set; }
        public Visit Visit { get; set; } = null!;
    }
}