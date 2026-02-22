namespace eweb.Domain.Entities.Progress;

public class UserLessonProgress
{
    public string UserId { get; private set; }
    public int LessonId { get; private set; }

    public DateTime OpenedAt { get; private set; }

    private UserLessonProgress() { }

    public UserLessonProgress(string userId, int lessonId)
    {
        UserId = userId;
        LessonId = lessonId;
        OpenedAt = DateTime.UtcNow;
    }
}