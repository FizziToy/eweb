namespace eweb.Domain.Entities.Exercises;

public class InteractiveExercise
{
    public int Id { get; private set; }

    public int LessonId { get; private set; }

    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }

    public int Order { get; private set; }

    public bool IsPublished { get; private set; }

    private readonly List<ExerciseTask> _tasks = new();
    public IReadOnlyCollection<ExerciseTask> Tasks => _tasks.AsReadOnly();

    private InteractiveExercise() { } // для EF

    public InteractiveExercise(int lessonId, string title, string? description, int order)
    {
        LessonId = lessonId;
        Title = title;
        Description = description;
        Order = order;
        IsPublished = false;
    }

    public void Update(string title, string? description, int order)
    {
        Title = title;
        Description = description;
        Order = order;
    }

    public void Publish() => IsPublished = true;

    public void Unpublish() => IsPublished = false;

    public void AddTask(ExerciseTask task)
    {
        _tasks.Add(task);
    }

    public void RemoveTask(ExerciseTask task)
    {
        _tasks.Remove(task);
    }
}