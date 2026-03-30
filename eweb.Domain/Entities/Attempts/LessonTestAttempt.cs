namespace eweb.Domain.Entities.Attempts;

public class LessonTestAttempt
{
    public int Id { get; private set; }

    public string UserId { get; private set; } = null!;

    public int LessonId { get; private set; }
    public Lesson Lesson { get; private set; } = null!;

    public DateTime StartedAt { get; private set; }

    public DateTime FinishedAt { get; private set; }

    public bool IsFinished { get; private set; }

    public double ResultPercent { get; private set; }

    private LessonTestAttempt() { }

    public LessonTestAttempt(string userId, int lessonId)
    {
        UserId = userId;
        LessonId = lessonId;
        StartedAt = DateTime.UtcNow;
    }

    public void Finish(double percent)
    {
        if (IsFinished)
            throw new InvalidOperationException("Спроба вже завершена.");

        ResultPercent = percent;
        IsFinished = true;
        FinishedAt = DateTime.UtcNow;
    }
}