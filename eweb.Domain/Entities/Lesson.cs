namespace eweb.Domain.Entities;

public class Lesson
{
    private const int MaxQuestions = 10;

    private readonly List<TheoryQuestion> _questions = new();

    public int Id { get; private set; }

    public int Number { get; private set; }

    public string Title { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    public string Content { get; private set; } = null!;

    public bool IsPublished { get; private set; }

    public DateOnly CreatedAt { get; private set; }

    public IReadOnlyCollection<TheoryQuestion> Questions => _questions.AsReadOnly();

    private Lesson() { } 

    public Lesson(int number, string title, string description, string content)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty.");

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be empty.");

        Number = number;
        Title = title;
        Description = description;
        Content = content;
        CreatedAt = DateOnly.FromDateTime(DateTime.UtcNow);
        IsPublished = false;
    }

    public void AddQuestion(TheoryQuestion question)
    {
        ArgumentNullException.ThrowIfNull(question);

        if (_questions.Count >= MaxQuestions)
            throw new InvalidOperationException("Lesson cannot have more than 10 questions.");

        question.Validate();

        _questions.Add(question);
    }

    public void Update(int number, string title, string description, string content)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.");

        Number = number;
        Title = title;
        Description = description;
        Content = content;
    }

    public void Publish()
    {
        IsPublished = true;
    }

    public void Unpublish()
    {
        IsPublished = false;
    }
}