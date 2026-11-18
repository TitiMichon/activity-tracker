using ActivityTracker.Web.Components;
using Bunit;

namespace ActivityTracker.Web.Tests;

public class TimeInputTests : WebTestBase
{
    [Theory]
    [InlineData("07:15", 7, 15)]
    [InlineData("00:00", 0, 0)]
    [InlineData("23:59:30", 23, 59)]
    public void Parses_browser_time_values(string text, int hour, int minute)
    {
        var time = TimeInput.Parse(text)!.Value;
        Assert.Equal((hour, minute), (time.Hour, time.Minute));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("7h15")]
    public void Rejects_empty_or_invalid_values(string? text) => Assert.Null(TimeInput.Parse(text));

    [Fact]
    public void Change_updates_the_bound_value_and_invalid_input_keeps_it()
    {
        TimeOnly? value = new TimeOnly(20, 0);
        var cut = Render<TimeInput>(p => p.Add(x => x.Value, value).Add(x => x.ValueChanged, t => value = t));
        Assert.Equal("20:00", cut.Find("input").GetAttribute("value"));

        cut.Find("input").Change("06:45");
        Assert.Equal(new TimeOnly(6, 45), value);

        cut.Find("input").Change("");
        Assert.Equal(new TimeOnly(6, 45), value);
    }
}
