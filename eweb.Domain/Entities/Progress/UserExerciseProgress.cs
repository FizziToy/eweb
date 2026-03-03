namespace eweb.Domain.Entities.Progress;

public class UserExerciseProgress
{
    public string UserId { get; private set; } = null!;
    public int ExerciseId { get; private set; }

    public int MaxCorrectTasks { get; private set; }

    public bool IsFullyCompleted { get; private set; }

    public int AdditionalAttempts { get; private set; }

    private UserExerciseProgress() { }

    public UserExerciseProgress(string userId, int exerciseId)
    {
        UserId = userId;
        ExerciseId = exerciseId;
        MaxCorrectTasks = 0;
        IsFullyCompleted = false;
        AdditionalAttempts = 0;
    }

    public int GetTotalAllowedAttempts()
    {
        return 10 + AdditionalAttempts;
    }

    public void UpdateFromAttempt(int correctTasksCount, bool fullyCompleted)
    {
        if (correctTasksCount > MaxCorrectTasks)
            MaxCorrectTasks = correctTasksCount;

        if (fullyCompleted)
            IsFullyCompleted = true;
    }

    public void AddAdditionalAttempts(int count)
    {
        if (count <= 0)
            throw new ArgumentException("Кількість додаткових спроб повинна бути більше 0.");

        AdditionalAttempts += count;
    }


}