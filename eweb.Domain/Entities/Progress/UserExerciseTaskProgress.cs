namespace eweb.Domain.Entities.Progress;

public class UserExerciseTaskProgress
{
    public string UserId { get; private set; }
    public int ExerciseTaskId { get; private set; }

    public DateTime CompletedAt { get; private set; }

    private UserExerciseTaskProgress() { }

    public UserExerciseTaskProgress(string userId, int taskId)
    {
        UserId = userId;
        ExerciseTaskId = taskId;
        CompletedAt = DateTime.UtcNow;
    }
}