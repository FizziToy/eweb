namespace eweb.Domain.Entities.Attempts;

public class ExerciseAttempt
{
    public int Id { get; private set; }

    public string UserId { get; private set; } = null!;
    public int ExerciseId { get; private set; }
    public DateTime StartedAt { get; private set; }
    public bool IsFinished { get; private set; }
    public DateTime? FinishedAt { get; private set; }

    private readonly List<TaskAttempt> _taskAttempts = [];
    public IReadOnlyCollection<TaskAttempt> TaskAttempts => _taskAttempts.AsReadOnly();

    private ExerciseAttempt() { }

    private ExerciseAttempt(string userId, int exerciseId)
    {
        UserId = userId;
        ExerciseId = exerciseId;
        StartedAt = DateTime.UtcNow;
    }

    public void EnsureTaskAttemptAllowed(int taskId)
    {
        var attempts = _taskAttempts
            .Where(x => x.ExerciseTaskId == taskId)
            .ToList();

        if (attempts.Any(x => x.IsCorrect))
            throw new InvalidOperationException(
                "Завдання вже виконано правильно.");

        if (attempts.Count >= 2)
            throw new InvalidOperationException(
                "Ви досягли максимальної кількості спроб для цього завдання.");
    }

    public static ExerciseAttempt Create(
    string userId,
    int exerciseId,
    int existingAttemptsCount,
    int allowedAttempts)
    {
        if (existingAttemptsCount >= allowedAttempts)
            throw new InvalidOperationException(
                "Досягнуто максимальної кількості спроб вправ.");

        return new ExerciseAttempt(userId, exerciseId);
    }

    public int GetCorrectTasksCount()
    {
        return _taskAttempts
            .Where(x => x.IsCorrect)
            .Select(x => x.ExerciseTaskId)
            .Distinct()
            .Count();
    }

    public bool IsFullyCompleted(IEnumerable<int> allTaskIds)
    {
        if (!IsFinished)
            return false;

        return allTaskIds.All(taskId =>
            _taskAttempts
                .Where(x => x.ExerciseTaskId == taskId)
                .Any(x => x.IsCorrect));
    }

    public void RegisterTaskAttempt(int taskId, bool isCorrect)
    {
        if (IsFinished)
            throw new InvalidOperationException(
                "Неможливо відповідати після завершення запуску.");

        EnsureTaskAttemptAllowed(taskId);

        var attempt = new TaskAttempt(taskId, isCorrect);
        _taskAttempts.Add(attempt);
    }

    public void Finish()
    {
        if (IsFinished)
            throw new InvalidOperationException(
                "Запуск вже завершений.");

        IsFinished = true;
        FinishedAt = DateTime.UtcNow;
    }
}