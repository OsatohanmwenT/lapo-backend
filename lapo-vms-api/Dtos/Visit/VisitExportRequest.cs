using System;
using lapo_vms_api.Model;

namespace lapo_vms_api.Dtos.Visit;

public class VisitExportRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public VisitStatus? Status { get; set; }
    public ExportType Format { get; set; } = ExportType.Csv;
}
