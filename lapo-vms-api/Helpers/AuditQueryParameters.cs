namespace lapo_vms_api.Helpers;

public class AuditQueryParameters
{
    private const int MaxPageSize = 100;
    private int _pageSize = 20;
    private int _pageNumber = 1;

    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (value < 1) { _pageSize = 20; return; }
            _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }
    }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? EventType { get; set; }
    public Guid? VisitorId { get; set; }
}
