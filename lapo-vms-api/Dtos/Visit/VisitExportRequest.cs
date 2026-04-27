using System;

namespace lapo_vms_api.Dtos.Visit;

public class VisitExportRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public ExportType Format { get; set; } = ExportType.Csv;
}
