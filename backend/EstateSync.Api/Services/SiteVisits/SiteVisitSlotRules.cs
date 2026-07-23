using System;
using System.Collections.Generic;
using System.Linq;

namespace EstateSync.Api.Services.SiteVisits;

public static class SiteVisitSlotRules
{
    public const int DurationMinutes = 60;
    public const int FirstSlotHour = 9;
    public const int LastSlotHour = 18;

    public static IReadOnlyCollection<TimeOnly> AllSlots { get; } =
        Enumerable.Range(FirstSlotHour, LastSlotHour - FirstSlotHour + 1)
            .Select(hour => new TimeOnly(hour, 0))
            .ToArray();

    public static bool IsSupported(TimeOnly time) =>
        time.Minute == 0 &&
        time.Second == 0 &&
        time.Hour >= FirstSlotHour &&
        time.Hour <= LastSlotHour;

    public static DateTime CurrentLocalDateTime()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
    }

    public static bool IsFuture(DateTime date, TimeOnly time)
    {
        var localNow = CurrentLocalDateTime();
        return date.Date.Add(time.ToTimeSpan()) > localNow;
    }
}
