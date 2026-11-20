using HeatMapStreak.Web.Data;
using HeatMapStreak.Web.Models;
using HeatMapStreak.Web.Services;
using Xunit;

namespace HeatMapStreak.Tests;

public class HabitServiceTests : IDisposable
{
    private const string Owner = TestDatabase.DefaultUserId;

    private readonly TestDatabase _db = new();

    public void Dispose() => _db.Dispose();

    private HabitService CreateService() => new(_db.CreateContext());

    private async Task<Habit> CreateHabitAsync(string userId = Owner, string name = "Water")
    {
        await using var db = _db.CreateContext();
        var habit = new Habit { Name = name, UserId = userId, Color = "#4CAF50" };
        db.Habits.Add(habit);
        await db.SaveChangesAsync();
        return habit;
    }

    [Fact]
    public async Task ToggleDay_Twice_TurnsTheDayOnThenOff()
    {
        var habit = await CreateHabitAsync();
        var service = CreateService();
        var date = new DateOnly(2026, 3, 5);

        Assert.True(await service.ToggleDayAsync(habit.Id, date, Owner));

        var afterFirst = await service.GetYearEntriesAsync(habit.Id, 2026, Owner);
        Assert.True(afterFirst.TryGetValue(date, out var on) && on);

        Assert.True(await service.ToggleDayAsync(habit.Id, date, Owner));

        var afterSecond = await service.GetYearEntriesAsync(habit.Id, 2026, Owner);
        Assert.True(afterSecond.TryGetValue(date, out var off) && !off);
    }

    [Fact]
    public async Task ToggleDay_WithForeignUserId_DoesNothing()
    {
        var habit = await CreateHabitAsync();
        var service = CreateService();
        var date = new DateOnly(2026, 3, 5);

        Assert.False(await service.ToggleDayAsync(habit.Id, date, "someone-else"));

        var entries = await service.GetYearEntriesAsync(habit.Id, 2026, "someone-else");
        Assert.Empty(entries);
    }

    [Fact]
    public async Task GetYearEntries_OnlyReturnsThatYearAndThatOwner()
    {
        var habit = await CreateHabitAsync();
        var service = CreateService();

        await service.ToggleDayAsync(habit.Id, new DateOnly(2025, 6, 1), Owner);
        await service.ToggleDayAsync(habit.Id, new DateOnly(2026, 6, 1), Owner);

        var currentYear = await service.GetYearEntriesAsync(habit.Id, 2026, Owner);
        Assert.Single(currentYear);
        Assert.True(currentYear.ContainsKey(new DateOnly(2026, 6, 1)));

        var otherOwner = await service.GetYearEntriesAsync(habit.Id, 2026, "someone-else");
        Assert.Empty(otherOwner);
    }

    [Fact]
    public async Task GetAggregatedYearData_CountsHabitsPerDay_IgnoringUnfinishedOnes()
    {
        var first = await CreateHabitAsync(name: "Water");
        var second = await CreateHabitAsync(name: "Read");
        var service = CreateService();
        var day = new DateOnly(2026, 7, 4);

        await service.ToggleDayAsync(first.Id, day, Owner);
        await service.ToggleDayAsync(second.Id, day, Owner);

        // Toggle it back off: unfinished days must not contribute to the aggregate.
        await service.ToggleDayAsync(second.Id, day, Owner);

        var aggregated = await service.GetAggregatedYearDataAsync(Owner, 2026);

        Assert.Equal(1, aggregated[day]);
    }

    [Fact]
    public async Task DeleteHabit_RemovesItsDayEntries()
    {
        var habit = await CreateHabitAsync();
        var service = CreateService();
        var date = new DateOnly(2026, 8, 9);
        await service.ToggleDayAsync(habit.Id, date, Owner);

        await service.DeleteHabitAsync(habit.Id, Owner);

        Assert.Null(await service.GetHabitByIdAsync(habit.Id, Owner));
        Assert.Empty(await service.GetYearEntriesAsync(habit.Id, 2026, Owner));
    }

    [Fact]
    public async Task DeleteHabit_WithForeignUserId_LeavesItAlone()
    {
        var habit = await CreateHabitAsync();

        await using (var db = _db.CreateContext())
            await new HabitService(db).DeleteHabitAsync(habit.Id, "someone-else");

        await using var check = _db.CreateContext();
        Assert.NotNull(await check.Habits.FindAsync(habit.Id));
    }

    [Fact]
    public async Task GetUserHabits_ReturnsOnlyThatUsersHabits()
    {
        _db.EnsureUser("someone-else");
        await CreateHabitAsync(userId: Owner, name: "Mine");
        await CreateHabitAsync(userId: "someone-else", name: "Theirs");

        await using var db = _db.CreateContext();
        var habits = await new HabitService(db).GetUserHabitsAsync(Owner);

        Assert.Single(habits);
        Assert.Equal("Mine", habits[0].Name);
    }
}
