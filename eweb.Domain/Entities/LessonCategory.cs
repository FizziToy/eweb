namespace eweb.Domain.Entities;

public class LessonCategory
{
    public int Id { get; private set; }

    public string Name { get; private set; } = null!;

    private readonly List<Lesson> _lessons = new();

    public IReadOnlyCollection<Lesson> Lessons => _lessons.AsReadOnly();

    private LessonCategory() { }

    public LessonCategory(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Назва категорії не може бути порожньою.");

        Name = name.Trim();
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Назва категорії не може бути порожньою.");

        Name = name.Trim();
    }
}