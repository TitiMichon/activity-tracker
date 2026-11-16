using ActivityTracker.Core.Models;

namespace ActivityTracker.Core.Services;

/// <summary>Ready-made activities to get started quickly.</summary>
public static class ActivityTemplates
{
    public static IReadOnlyList<(string Key, Func<DateOnly, Activity> Create)> All { get; } =
    [
        ("music", start => new Activity
        {
            Name = "Music practice", Icon = "🎸", Color = "#8b5cf6", Kind = ActivityKind.Quantity, Unit = "min", DefaultValue = 30,
            StartDate = start, Schedule = new Schedule { Frequency = Frequency.Daily },
            Goal = new Goal { Period = GoalPeriod.Week, Metric = GoalMetric.Sum, Target = 150 },
        }),
        ("sport", start => new Activity
        {
            Name = "Sport", Icon = "🏃", Color = "#ef4444", Kind = ActivityKind.Check, StartDate = start,
            Schedule = new Schedule { Frequency = Frequency.Weekly, DaysOfWeek = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday] },
            Goal = new Goal { Period = GoalPeriod.Week, Metric = GoalMetric.Count, Target = 3 },
        }),
        ("reading", start => new Activity
        {
            Name = "Reading", Icon = "📚", Color = "#f59e0b", Kind = ActivityKind.Quantity, Unit = "pages", DefaultValue = 20,
            StartDate = start, Goal = new Goal { Period = GoalPeriod.Month, Metric = GoalMetric.Sum, Target = 400 },
        }),
        ("mood", start => new Activity
        {
            Name = "Mood", Icon = "🙂", Color = "#14b8a6", Kind = ActivityKind.Scale, StartDate = start,
            Schedule = new Schedule { Frequency = Frequency.Daily },
            Options =
            [
                new ActivityOption { Id = "awful", Label = "Awful", Icon = "😫", Color = "#dc2626", Score = 1 },
                new ActivityOption { Id = "bad", Label = "Bad", Icon = "😕", Color = "#f97316", Score = 2 },
                new ActivityOption { Id = "ok", Label = "Okay", Icon = "😐", Color = "#eab308", Score = 3 },
                new ActivityOption { Id = "good", Label = "Good", Icon = "🙂", Color = "#84cc16", Score = 4 },
                new ActivityOption { Id = "great", Label = "Great", Icon = "😄", Color = "#16a34a", Score = 5 },
            ],
        }),
        ("meditation", start => new Activity
        {
            Name = "Meditation", Icon = "🧘", Color = "#0ea5e9", Kind = ActivityKind.Quantity, Unit = "min", DefaultValue = 10,
            StartDate = start, Schedule = new Schedule { Frequency = Frequency.Daily },
        }),
        ("water", start => new Activity
        {
            Name = "Water", Icon = "💧", Color = "#3b82f6", Kind = ActivityKind.Quantity, Unit = "glasses", DefaultValue = 1,
            StartDate = start, Goal = new Goal { Period = GoalPeriod.Day, Metric = GoalMetric.Sum, Target = 8 },
        }),
    ];

    public static Activity Create(string key, DateOnly start) =>
        All.First(t => t.Key == key).Create(start);
}
