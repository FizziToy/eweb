using eweb.Domain.Entities.Attempts;

namespace eweb.Tests;

public class ExerciseAttemptTests
{
    [Fact]
    public void AllowTwoIncorrectAttempts()
    {
        var attempt = ExerciseAttempt.Create("user1", 1, 0, 10);

        attempt.RegisterTaskAttempt(5, false);
        attempt.RegisterTaskAttempt(5, false);

        Assert.Throws<InvalidOperationException>(() =>
            attempt.RegisterTaskAttempt(5, false));
    }

    [Fact]
    public void BlockAfterCorrectAns()
    {
        var attempt = ExerciseAttempt.Create("user1", 1, 0, 10);

        attempt.RegisterTaskAttempt(5, true);

        Assert.Throws<InvalidOperationException>(() =>
            attempt.RegisterTaskAttempt(5, false));
    }

    [Fact]
    public void HaveSeparateLimits()
    {
        var attempt = ExerciseAttempt.Create("user1", 1, 0, 10);

        attempt.RegisterTaskAttempt(1, false);
        attempt.RegisterTaskAttempt(2, false);

        var ex = Record.Exception(() =>
            attempt.RegisterTaskAttempt(1, false));

        Assert.Null(ex);
    }

    [Fact]
    public void MoreThanTenAttempts()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ExerciseAttempt.Create("user1", 1, 10, 10));
    }

    [Fact]
    public void LessThanTenAttempts()
    {
        var attempt = ExerciseAttempt.Create("user1", 1, 5, 10);

        Assert.NotNull(attempt);
    }

    [Fact]
    public void CountCorrectTasksDistinctly()
    {
        var attempt = ExerciseAttempt.Create("user1", 1, 0, 10);

        attempt.RegisterTaskAttempt(1, true);
        attempt.RegisterTaskAttempt(2, false);
        attempt.RegisterTaskAttempt(2, true);

        var correctCount = attempt.GetCorrectTasksCount();

        Assert.Equal(2, correctCount);
    }

    [Fact]
    public void FullyCompletedOnlyAfterFinish()
    {
        var attempt = ExerciseAttempt.Create("user1", 1, 0, 10);

        attempt.RegisterTaskAttempt(1, true);
        attempt.RegisterTaskAttempt(2, true);
        attempt.RegisterTaskAttempt(3, true);

        Assert.False(attempt.IsFullyCompleted(new[] { 1, 2, 3 }));

        attempt.Finish();

        Assert.True(attempt.IsFullyCompleted(new[] { 1, 2, 3 }));
    }

    [Fact]
    public void CannotRegisterAfterFinish()
    {
        var attempt = ExerciseAttempt.Create("user1", 1, 0, 10);

        attempt.RegisterTaskAttempt(1, false);
        attempt.Finish();

        Assert.Throws<InvalidOperationException>(() =>
            attempt.RegisterTaskAttempt(1, false));
    }

    [Fact]
    public void CannotFinishTwice()
    {
        var attempt = ExerciseAttempt.Create("user1", 1, 0, 10);

        attempt.Finish();

        Assert.Throws<InvalidOperationException>(() =>
            attempt.Finish());
    }
}