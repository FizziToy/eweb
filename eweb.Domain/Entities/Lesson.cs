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
        if (number <= 0)
            throw new ArgumentException("Номер уроку має бути більший за 0.");

        Number = number;
        Title = title?.Trim() ?? string.Empty;
        Description = description?.Trim() ?? string.Empty;
        Content = content?.Trim() ?? string.Empty;

        CreatedAt = DateOnly.FromDateTime(DateTime.UtcNow);
        IsPublished = false;
    }

    private void EnsureEditable()
    {
        if (IsPublished)
            throw new InvalidOperationException(
                "Опублікований урок не можна редагувати. Спочатку зніміть його з публікації."
            );
    }

    public void Update(int number, string title, string description, string content)
    {
        EnsureEditable();

        if (number <= 0)
            throw new ArgumentException("Номер уроку має бути більший за 0.");

        Number = number;
        Title = title?.Trim() ?? string.Empty;
        Description = description?.Trim() ?? string.Empty;
        Content = content?.Trim() ?? string.Empty;
    }

    public void AddQuestion(TheoryQuestion question)
    {
        EnsureEditable();

        ArgumentNullException.ThrowIfNull(question);

        if (_questions.Count >= MaxQuestions)
            throw new InvalidOperationException(
                $"Урок не може мати більше {MaxQuestions} питань."
            );

        question.Validate();

        _questions.Add(question);
    }

    public void RemoveQuestion(int questionId)
    {
        EnsureEditable();

        var question = _questions.FirstOrDefault(q => q.Id == questionId);

        if (question == null)
            throw new InvalidOperationException("Питання не знайдено.");

        _questions.Remove(question);
    }

    public void Publish()
    {
        if (IsPublished)
            throw new InvalidOperationException("Урок вже опублікований.");

        if (string.IsNullOrWhiteSpace(Title))
            throw new InvalidOperationException("Назва уроку не може бути порожньою.");

        if (string.IsNullOrWhiteSpace(Description))
            throw new InvalidOperationException("Опис уроку не може бути порожнім.");

        if (string.IsNullOrWhiteSpace(Content))
            throw new InvalidOperationException("Контент уроку не може бути порожнім.");

        if (!_questions.Any())
            throw new InvalidOperationException("Урок не можна опублікувати без тестових питань.");

        IsPublished = true;
    }

    public void Unpublish()
    {
        if (!IsPublished)
            throw new InvalidOperationException("Урок вже не опублікований.");

        IsPublished = false;
    }
}