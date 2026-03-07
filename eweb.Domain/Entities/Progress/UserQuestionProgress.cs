namespace eweb.Domain.Entities.Progress;

public class UserQuestionProgress
{
    public string UserId { get; private set; }
    public int QuestionId { get; private set; }

    public DateTime CompletedAt { get; private set; }

    public TheoryQuestion Question { get; private set; } = null!;

    private UserQuestionProgress() { }

    public UserQuestionProgress(string userId, int questionId)
    {
        UserId = userId;
        QuestionId = questionId;
        CompletedAt = DateTime.UtcNow;
    }
}