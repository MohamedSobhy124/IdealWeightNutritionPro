namespace IdealWeightNutrition.Domain.Time;

/// <summary>UAE business clock (Asia/Dubai, UTC+4, no DST).</summary>
public static class UaeDateTime
{
    private static readonly TimeZoneInfo Zone = ResolveZone();

    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);

    public static DateTime Today => Now.Date;

    public static DateTime ToUaeLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(
            utc.Kind switch
            {
                DateTimeKind.Utc => utc,
                DateTimeKind.Local => utc.ToUniversalTime(),
                _ => DateTime.SpecifyKind(utc, DateTimeKind.Utc)
            },
            Zone);

    private static TimeZoneInfo ResolveZone()
    {
        foreach (var id in new[] { "Asia/Dubai", "Arabian Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone(
            "Asia/Dubai",
            TimeSpan.FromHours(4),
            "Gulf Standard Time",
            "Gulf Standard Time");
    }
}
