namespace eweb.Domain.Entities;

public class TheoryQuestion
{
    private const int MinAnswers = 2;
    private const int MaxAnswers = 9;

    private const int MinCorrectAnswers = 1;
    private const int MaxCorrectAnswers = 3;

    private readonly List<AnswerOption> _answerOptions = new();

    public int Id { get; private set; }

    public string QuestionText { get; private set; } = null!;

    public int LessonId { get; private set; }

    public IReadOnlyCollection<AnswerOption> AnswerOptions => _answerOptions.AsReadOnly();
    private TheoryQuestion() { }

    public TheoryQuestion(string questionText, int lessonId)
    {
        if (string.IsNullOrWhiteSpace(questionText))
            throw new ArgumentException("Question text cannot be empty.");

        QuestionText = questionText;
        LessonId = lessonId;
    }

    public void AddAnswerOption(string text, bool isCorrect)
    {
        if (_answerOptions.Count >= MaxAnswers)
            throw new InvalidOperationException($"Cannot have more than {MaxAnswers} answers.");

        _answerOptions.Add(new AnswerOption(text, isCorrect));
    }

    public void Validate()
    {
        if (_answerOptions.Count < MinAnswers)
            throw new InvalidOperationException($"A question must have at least {MinAnswers} answers.");

        if (_answerOptions.Count > MaxAnswers)
            throw new InvalidOperationException($"A question cannot have more than {MaxAnswers} answers.");

        var correctCount = _answerOptions.Count(a => a.IsCorrect);

        if (correctCount < MinCorrectAnswers)
            throw new InvalidOperationException($"At least {MinCorrectAnswers} correct answer is required.");

        if (correctCount > MaxCorrectAnswers)
            throw new InvalidOperationException($"No more than {MaxCorrectAnswers} correct answers are allowed.");

        var incorrectCount = _answerOptions.Count(a => !a.IsCorrect);

        if (incorrectCount < 1)
            throw new InvalidOperationException("At least one answer must be incorrect.");
    }
}