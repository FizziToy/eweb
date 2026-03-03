namespace eweb.Domain.Entities.Attempts;

public class TaskAttempt
{
    public int Id { get; private set; }

    public int ExerciseAttemptId { get; private set; }
    public int ExerciseTaskId { get; private set; }

    public bool IsCorrect { get; private set; }

    public DateTime AttemptedAt { get; private set; }

    private TaskAttempt() { }

    public TaskAttempt(int exerciseTaskId, bool isCorrect)
    {
        ExerciseTaskId = exerciseTaskId;
        IsCorrect = isCorrect;
        AttemptedAt = DateTime.UtcNow;
    }
}
