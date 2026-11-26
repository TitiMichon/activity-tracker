namespace ActivityTracker.Core.Services;

/// <summary>
/// Which activities were already reminded today, stored as "2026-11-16|id1,id2" so a reminder is sent once per day
/// even when the app restarts. Entries from another day are ignored.
/// </summary>
public static class ReminderLog
{
    public static HashSet<Guid> Parse(string? stored, DateOnly today)
    {
        var ids = new HashSet<Guid>();
        if (stored?.Split('|') is [var day, var list] && day == Format(today))
        {
            foreach (var id in list.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (Guid.TryParse(id, out var guid))
                {
                    ids.Add(guid);
                }
            }
        }

        return ids;
    }

    public static string Format(DateOnly today, IEnumerable<Guid> ids) => $"{Format(today)}|{string.Join(',', ids)}";

    private static string Format(DateOnly day) => day.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
}
