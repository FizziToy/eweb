namespace eweb.Domain.Entities.Exercises;

public class InteractiveExercise
{
    public int Id { get; private set; }

    public int LessonId { get; private set; }

    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }

    public int Order { get; private set; }

    public bool IsPublished { get; private set; }

    private readonly List<ExerciseTask> _tasks = [];
    public IReadOnlyCollection<ExerciseTask> Tasks => _tasks.AsReadOnly();

    private InteractiveExercise() { } // для EF

    public InteractiveExercise(int lessonId, string title, string? description, int order)
    {
        Validate(lessonId, title, order);

        LessonId = lessonId;
        Title = title.Trim();
        Description = description?.Trim();
        Order = order;
        IsPublished = false;
    }

    public void EnsureCanBePublished()
    {
        if (Tasks.Count < 3 || Tasks.Count > 5)
            throw new InvalidOperationException(
                "Вправа повинна мiстити 3–5 завдань перед публiкацiєю.");
    }

    public void Update(string title, string? description, int order)
    {
        Validate(LessonId, title, order);

        Title = title.Trim();
        Description = description?.Trim();
        Order = order;
    }

    public void Publish()
    {
        EnsureCanBePublished();

        IsPublished = true;
    }

    public void Unpublish() => IsPublished = false;

    public void AddTask(ExerciseTask task)
    {
        ArgumentNullException.ThrowIfNull(task);

        _tasks.Add(task);
    }

    public void RemoveTask(ExerciseTask task)
    {
        _tasks.Remove(task);
    }

    public void ClearTasks()
    {
        _tasks.Clear();
    }

    public void EnsureCanBeEdited()
    {
        if (IsPublished)
            throw new InvalidOperationException(
                "Неможливо редагувати опубліковану вправу. Спочатку скасуйте публікацію.");
    }

    public static void EnsureLessonExerciseLimit(int existingCount)
    {
        if (existingCount >= 2)
            throw new InvalidOperationException(
                "Урок не може містити більше 2 вправ.");
    }

    private static void Validate(int lessonId, string title, int order)
    {
        if (lessonId <= 0)
            throw new ArgumentException("Урок не задано.");

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Назва вправи не може бути порожньою.");

        if (order <= 0)
            throw new ArgumentException("Порядок вправи має бути більший за 0.");
    }
}
