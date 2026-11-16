namespace ActivityTracker.Core.Services;

/// <summary>Date helpers used by the day / week / month views.</summary>
public static class CalendarMath
{
    public static DateOnly StartOfWeek(DateOnly date, DayOfWeek firstDay)
    {
        var diff = (7 + (int)date.DayOfWeek - (int)firstDay) % 7;
        return date.AddDays(-diff);
    }

    public static DateOnly[] WeekDays(DateOnly date, DayOfWeek firstDay)
    {
        var start = StartOfWeek(date, firstDay);
        return Enumerable.Range(0, 7).Select(start.AddDays).ToArray();
    }

    /// <summary>Full weeks (rows of 7 days) covering the given month.</summary>
    public static List<DateOnly[]> MonthWeeks(int year, int month, DayOfWeek firstDay)
    {
        var first = new DateOnly(year, month, 1);
        var last = first.AddMonths(1).AddDays(-1);
        var weeks = new List<DateOnly[]>();
        for (var start = StartOfWeek(first, firstDay); start <= last; start = start.AddDays(7))
        {
            weeks.Add(Enumerable.Range(0, 7).Select(start.AddDays).ToArray());
        }

        return weeks;
    }

    public static DateOnly StartOfMonth(DateOnly date) => new(date.Year, date.Month, 1);

    public static DateOnly EndOfMonth(DateOnly date) => StartOfMonth(date).AddMonths(1).AddDays(-1);
}
