using HeatMapStreak.Web.Data;
using HeatMapStreak.Web.Models;
using HeatMapStreak.Web.Services;
using Xunit;

namespace HeatMapStreak.Tests;

public class StreakServiceTests : IDisposable
{
    private readonly TestDatabase _db = new();

    public void Dispose() => _db.Dispose();

    private async Task<Habit> CreateHabitAsync(params DateOnly[] completedDates)
    {
        await using var db = _db.CreateContext();

        var habit = new Habit { Name = "Water", UserId = TestDatabase.DefaultUserId, Color = "#4CAF50" };
        db.Habits.Add(habit);
        await db.SaveChangesAsync();

        foreach (var date in completedDates)
            db.DayEntries.Add(new DayEntry { HabitId = habit.Id, Date = date, IsCompleted = true });

        await db.SaveChangesAsync();
        return habit;
    }

    private async Task<StreakInfo> GetStreakAsync(
        int habitId, string userId = TestDatabase.DefaultUserId)
    {
        await using var db = _db.CreateContext();
        return await new StreakService(db).GetStreakAsync(habitId, userId);
    }

    [Fact]
    public async Task UnknownHabit_ReturnsEmptyStreak()
    {
        var streak = await GetStreakAsync(habitId: 12345);

        Assert.Equal(0, streak.CurrentStreak);
        Assert.Equal(0, streak.LongestStreak);
    }

    [Fact]
    public async Task HabitWithoutEntries_ReturnsZeroStreaks()
    {
        var habit = await CreateHabitAsync();

        var streak = await GetStreakAsync(habit.Id);

        Assert.Equal(0, streak.CurrentStreak);
        Assert.Equal(0, streak.LongestStreak);
    }

    [Fact]
    public async Task CompletedToday_CountsAsCurrentAndLongestStreak()
    {
        var habit = await CreateHabitAsync(DateOnly.FromDateTime(DateTime.UtcNow));

        var streak = await GetStreakAsync(habit.Id);

        Assert.Equal(1, streak.CurrentStreak);
        Assert.Equal(1, streak.LongestStreak);
    }

    [Fact]
    public async Task CompletedYesterdayOnly_StillKeepsCurrentStreakAlive()
    {
        var habit = await CreateHabitAsync(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1));

        var streak = await GetStreakAsync(habit.Id);

        Assert.Equal(1, streak.CurrentStreak);
        Assert.Equal(1, streak.LongestStreak);
    }

    [Fact]
    public async Task MissedYesterday_BreaksCurrentStreakButKeepsLongest()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        // Three consecutive days that ended two days ago: today and yesterday are empty.
        var habit = await CreateHabitAsync(today.AddDays(-4), today.AddDays(-3), today.AddDays(-2));

        var streak = await GetStreakAsync(habit.Id);

        Assert.Equal(0, streak.CurrentStreak);
        Assert.Equal(3, streak.LongestStreak);
    }

    [Fact]
    public async Task LongestRunWinsOverCurrentRun()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var habit = await CreateHabitAsync(
            today.AddDays(-20), today.AddDays(-19),          // older 2-day run
            today.AddDays(-10), today.AddDays(-9), today.AddDays(-8),
            today.AddDays(-7), today.AddDays(-6),             // 5-day run
            today);                                           // current 1-day run

        var streak = await GetStreakAsync(habit.Id);

        Assert.Equal(1, streak.CurrentStreak);
        Assert.Equal(5, streak.LongestStreak);
    }

    [Fact]
    public async Task OtherUsersHabit_IsNotVisible()
    {
        var habit = await CreateHabitAsync(DateOnly.FromDateTime(DateTime.UtcNow));

        var streak = await GetStreakAsync(habit.Id, userId: "someone-else");

        Assert.Equal(0, streak.CurrentStreak);
        Assert.Equal(0, streak.LongestStreak);
    }
}
