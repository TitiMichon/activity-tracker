using ActivityTracker.Core.Models;

namespace ActivityTracker.Core.Services;

public static class ActivityValidator
{
    public static IReadOnlyList<string> Validate(Activity activity)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(activity.Name))
        {
            errors.Add("Name is required.");
        }

        if (string.IsNullOrWhiteSpace(activity.Icon))
        {
            errors.Add("Icon is required.");
        }

        if (!EntryAppearance.IsHexColor(activity.Color))
        {
            errors.Add("Color must be a hex value like #4f46e5.");
        }

        if (activity.Kind == ActivityKind.Scale)
        {
            if (activity.Options.Count == 0)
            {
                errors.Add("A scale activity needs at least one option.");
            }
            else if (activity.Options.Any(o => string.IsNullOrWhiteSpace(o.Label)))
            {
                errors.Add("Every option needs a label.");
            }
        }

        if (activity.Schedule.Frequency == Frequency.EveryNDays && activity.Schedule.Interval < 1)
        {
            errors.Add("Interval must be at least 1 day.");
        }

        if (activity.Schedule.Frequency == Frequency.Monthly && activity.Schedule.DayOfMonth is < 1 or > 31)
        {
            errors.Add("Day of month must be between 1 and 31.");
        }

        if (activity.Schedule.TimesPerDay is < 1 or > 100)
        {
            errors.Add("Times per day must be between 1 and 100.");
        }

        if (activity.Goal is { Target: <= 0 })
        {
            errors.Add("Objective target must be greater than 0.");
        }

        if (activity.Deadline is { } deadline && deadline < activity.StartDate)
        {
            errors.Add("Deadline must be on or after the start date.");
        }

        return errors;
    }
}
