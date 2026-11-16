using ActivityTracker.Core.Models;
using ActivityTracker.Core.Persistence;
using ActivityTracker.Core.Services;
using static ActivityTracker.Core.Tests.TestData;

namespace ActivityTracker.Core.Tests;

public class TrackerJsonTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("null")]
    public void Empty_input_gives_empty_data(string? json)
    {
        var data = TrackerJson.Deserialize(json);
        Assert.Empty(data.Activities);
        Assert.Equal(TrackerData.CurrentSchemaVersion, data.SchemaVersion);
    }

    [Fact]
    public void Invalid_json_throws_format_exception()
    {
        Assert.Throws<FormatException>(() => TrackerJson.Deserialize("{not json"));
    }

    [Fact]
    public void Newer_schema_is_rejected()
    {
        var ex = Assert.Throws<FormatException>(() => TrackerJson.Deserialize("""{"schemaVersion": 999}"""));
        Assert.Contains("newer", ex.Message);
    }

    [Fact]
    public void Enums_and_dates_are_human_readable()
    {
        var a = Check(frequency: Frequency.Weekly, start: Monday);
        a.Schedule.DaysOfWeek = [DayOfWeek.Friday];
        var json = TrackerJson.Serialize(new TrackerData { Activities = [a] });

        Assert.Contains("\"kind\":\"Check\"", json);
        Assert.Contains("\"Friday\"", json);
        Assert.Contains("\"2026-11-16\"", json);
    }

    [Fact]
    public void Clone_is_deep()
    {
        var mood = Mood();
        var copy = TrackerJson.Clone(mood);
        copy.Options[0].Label = "changed";
        Assert.Equal("Bad", mood.Options[0].Label);
    }
}

public class EntryAppearanceTests
{
    [Fact]
    public void Entry_override_beats_option_which_beats_activity()
    {
        var mood = Mood();
        mood.Icon = "🙂";
        mood.Color = "#123456";

        var plain = new Entry { ActivityId = mood.Id };
        var withOption = new Entry { ActivityId = mood.Id, OptionId = "great" };
        var overridden = new Entry { ActivityId = mood.Id, OptionId = "great", Icon = "🎉", Color = "#abcdef" };

        Assert.Equal(("🙂", "#123456"), (EntryAppearance.Icon(mood, plain), EntryAppearance.Color(mood, plain)));
        Assert.Equal(("😄", "#00ff00"), (EntryAppearance.Icon(mood, withOption), EntryAppearance.Color(mood, withOption)));
        Assert.Equal(("🎉", "#abcdef"), (EntryAppearance.Icon(mood, overridden), EntryAppearance.Color(mood, overridden)));
    }

    [Fact]
    public void Value_text_per_kind()
    {
        var q = Quantity();
        Assert.Equal("12.5 min", EntryAppearance.ValueText(q, new Entry { Value = 12.5 }));
        Assert.Equal("Great", EntryAppearance.ValueText(Mood(), new Entry { OptionId = "great" }));
        Assert.Equal("✓", EntryAppearance.ValueText(Check(), new Entry()));
    }

    [Theory]
    [InlineData("#ffffff", "#111111")]
    [InlineData("#fff", "#111111")]
    [InlineData("#000000", "#ffffff")]
    [InlineData("#4f46e5", "#ffffff")]
    [InlineData("garbage", "#000000")]
    public void Contrast_text(string background, string expected)
    {
        Assert.Equal(expected, EntryAppearance.ContrastText(background));
    }
}

public class ActivityValidatorTests
{
    [Fact]
    public void Templates_are_all_valid()
    {
        Assert.All(ActivityTemplates.All, t => Assert.Empty(ActivityValidator.Validate(t.Create(Monday))));
    }

    [Fact]
    public void Reports_every_problem()
    {
        var a = new Activity
        {
            Name = "",
            Color = "blue",
            Kind = ActivityKind.Scale,
            StartDate = Monday,
            Deadline = Monday.AddDays(-1),
            Goal = new Goal { Target = 0 },
        };

        var errors = ActivityValidator.Validate(a);

        Assert.Equal(5, errors.Count);
    }
}
