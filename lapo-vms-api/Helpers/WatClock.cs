namespace lapo_vms_api.Helpers;

public static class WatClock
{
    private static readonly TimeZoneInfo _wat =
        TimeZoneInfo.FindSystemTimeZoneById("W. Central Africa Standard Time");

    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _wat);
}
