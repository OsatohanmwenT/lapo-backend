namespace lapo_vms_api.Dtos.Visitor;

public class VisitorExportRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public ExportType Format { get; set; } = ExportType.Csv;
}
