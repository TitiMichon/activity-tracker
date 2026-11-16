using ActivityTracker.Core.Services;
using static ActivityTracker.Core.Tests.TestData;

namespace ActivityTracker.Core.Tests;

public class HeatmapBuilderTests
{
    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(1, 10, 1)]
    [InlineData(2.5, 10, 1)]
    [InlineData(2.6, 10, 2)]
    [InlineData(7.5, 10, 3)]
    [InlineData(10, 10, 4)]
    [InlineData(5, 0, 0)]
    public void Level_is_proportional_to_max(double value, double max, int expected)
    {
        Assert.Equal(expected, HeatmapBuilder.Level(value, max));
    }

    [Fact]
    public void Grid_is_made_of_full_weeks_with_padding_outside_range()
    {
        var from = Monday.AddDays(2); // Wednesday
        var to = Monday.AddDays(9);   // next Wednesday

        var map = HeatmapBuilder.Build(null, [], from, to, DayOfWeek.Monday);

        Assert.Equal(2, map.Weeks.Count);
        Assert.Equal(Monday, map.Weeks[0][0].Date);
        Assert.False(map.Weeks[0][0].InRange);
        Assert.True(map.Weeks[0][2].InRange);
        Assert.Equal(8, map.Cells.Count());
    }

    [Fact]
    public void All_activities_counts_entries_per_day()
    {
        var a = Check();
        var b = Quantity();
        var map = HeatmapBuilder.Build(null, [On(a, Monday), On(b, Monday, 30), On(a, Monday.AddDays(1))], Monday, Monday.AddDays(6), DayOfWeek.Monday);

        var cells = map.Cells.ToList();
        Assert.Equal(2, cells[0].Value);
        Assert.Equal(4, cells[0].Level);
        Assert.Equal(1, cells[1].Value);
        Assert.Equal(2, cells[1].Level);
        Assert.Equal(0, cells[2].Level);
        Assert.Equal(2, map.ActiveDays);
        Assert.Equal(3, map.TotalEntries);
    }

    [Fact]
    public void Quantity_activity_sums_values_and_ignores_other_activities()
    {
        var a = Quantity();
        var other = Check();
        var map = HeatmapBuilder.Build(a, [On(a, Monday, 20), On(a, Monday, 40), On(other, Monday)], Monday, Monday, DayOfWeek.Monday);

        var cell = map.Cells.Single();
        Assert.Equal(60, cell.Value);
        Assert.Equal(2, cell.EntryCount);
        Assert.Equal(a.Color, cell.Color);
    }

    [Fact]
    public void Scale_activity_uses_closest_option_color()
    {
        var mood = Mood();
        var map = HeatmapBuilder.Build(mood, [On(mood, Monday, optionId: "great"), On(mood, Monday, optionId: "ok")], Monday, Monday.AddDays(1), DayOfWeek.Monday);

        var cells = map.Cells.ToList();
        Assert.Equal(4, cells[0].Value);       // average of 5 and 3
        Assert.Equal("#ffff00", cells[0].Color); // tie between Ok(3) and Great(5) → first option wins
        Assert.Equal(4, cells[0].Level);
        Assert.Equal(0, cells[1].Level);
    }

    [Fact]
    public void Scale_single_entry_maps_to_its_option_color()
    {
        var mood = Mood();
        var map = HeatmapBuilder.Build(mood, [On(mood, Monday, optionId: "bad")], Monday, Monday, DayOfWeek.Monday);
        Assert.Equal("#ff0000", map.Cells.Single().Color);
    }
}
