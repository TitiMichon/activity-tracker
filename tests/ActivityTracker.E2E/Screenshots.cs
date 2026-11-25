using ActivityTracker.Core.Models;
using ActivityTracker.Core.Persistence;
using ActivityTracker.Core.Services;
using Microsoft.Playwright;

namespace ActivityTracker.E2E;

/// <summary>
/// Captures screenshots of every page with realistic demo data (for docs and visual review).
/// Runs only when both E2E_APP_DIR and SCREENSHOT_DIR are set.
/// </summary>
[Collection(E2ECollection.Name)]
public class Screenshots(AppFixture app)
{
    public static string? OutputDir => Environment.GetEnvironmentVariable("SCREENSHOT_DIR");

    [ScreenshotFact]
    public async Task Capture_all_pages()
    {
        Directory.CreateDirectory(OutputDir!);
        var json = DemoData(DateOnly.FromDateTime(DateTime.Today));

        foreach (var (name, width, height) in new[] { ("desktop", 1280, 900), ("mobile", 390, 844) })
        {
            var page = await app.NewPageAsync();
            await page.SetViewportSizeAsync(width, height);
            await page.EvaluateAsync("data => localStorage.setItem('activity-tracker:data', data)", json);

            foreach (var path in new[] { "", "calendar", "calendar?view=week", "heatmap", "heatmap?separate=1", "activities" })
            {
                await page.GotoAsync(app.BaseUrl + path);
                await page.Locator("main.content h1").First.WaitForAsync();
                await page.WaitForTimeoutAsync(300);
                var file = $"{name}-{(path == "" ? "today" : path.Replace("?", "-").Replace("=", "-"))}.png";
                await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(OutputDir!, file), FullPage = true });
            }

            await page.GotoAsync(app.BaseUrl);
            await page.GetByRole(AriaRole.Button, new() { Name = "Log Mood" }).ClickAsync();
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(OutputDir!, $"{name}-quick-add-mood.png") });
            await page.Context.CloseAsync();
        }
    }

    private static string DemoData(DateOnly today)
    {
        var random = new Random(42);
        var start = today.AddDays(-200);
        var activities = new[] { "music", "sport", "reading", "mood", "meditation" }
            .Select(k => ActivityTemplates.Create(k, start)).ToList();
        var entries = new List<Entry>();
        for (var d = start; d <= today; d = d.AddDays(1))
        {
            foreach (var a in activities)
            {
                var chance = ScheduleCalculator.IsDue(a, d) ? 0.75 : 0.25;
                if (d == today || random.NextDouble() > chance)
                {
                    continue;
                }

                entries.Add(new Entry
                {
                    ActivityId = a.Id,
                    Timestamp = d.ToDateTime(new TimeOnly(random.Next(7, 22), random.Next(0, 60))),
                    Value = a.Kind == ActivityKind.Quantity ? random.Next(1, 7) * (a.DefaultValue ?? 1) / 2 : null,
                    OptionId = a.Kind == ActivityKind.Scale ? a.Options[Math.Min(4, random.Next(1, 6) + random.Next(0, 2) - 1)].Id : null,
                });
            }
        }

        entries.Add(new Entry { ActivityId = activities[0].Id, Timestamp = today.ToDateTime(new TimeOnly(8, 10)), Value = 45, Note = "Scales + new song" });
        entries.Add(new Entry { ActivityId = activities[3].Id, Timestamp = today.ToDateTime(new TimeOnly(8, 30)), OptionId = "good" });
        var morning = today.ToDateTime(new TimeOnly(7, 30));
        TodoItem Todo(string title, TodoList list, int daysAgo = 0, bool done = false) => new()
        {
            Title = title,
            List = list,
            Day = list == TodoList.Today ? today.AddDays(-daysAgo) : null,
            CreatedAt = morning.AddDays(-daysAgo),
            DoneAt = done ? morning.AddHours(1) : null,
        };
        var todos = new List<TodoItem>
        {
            Todo("Call the dentist", TodoList.Today, daysAgo: 1),
            Todo("Do the groceries", TodoList.Today),
            Todo("Reply to Sam", TodoList.Today, done: true),
            Todo("Learn the Blackbird chords", TodoList.LongTerm),
            Todo("Plan the summer holidays", TodoList.LongTerm),
        };
        return TrackerJson.Serialize(new TrackerData { Activities = activities, Entries = entries, Todos = todos });
    }
}

public sealed class ScreenshotFactAttribute : FactAttribute
{
    public ScreenshotFactAttribute()
    {
        if (string.IsNullOrEmpty(AppFixture.AppDir) || string.IsNullOrEmpty(Screenshots.OutputDir))
        {
            Skip = "Set E2E_APP_DIR and SCREENSHOT_DIR to capture screenshots.";
        }
    }
}
