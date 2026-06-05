using System;

namespace lapo_vms_api.Dtos.Visit;

public class VisitExportRequest
{
    public string? Search { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Status { get; set; }
    public ExportType Format { get; set; } = ExportType.Csv;
}
