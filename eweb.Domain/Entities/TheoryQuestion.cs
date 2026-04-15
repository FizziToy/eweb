namespace eweb.Domain.Entities;
public class TheoryQuestion
{
    private const int MinAnswers = 2;
    private const int MaxAnswers = 9;

    private const int MinCorrectAnswers = 1;
    private const int MaxCorrectAnswers = 8;

    private readonly List<AnswerOption> _answerOptions = new();

    public int Id { get; private set; }

    public string QuestionText { get; private set; } = null!;

    public int LessonId { get; private set; }

    public Lesson Lesson { get; private set; } = null!;

    public IReadOnlyCollection<AnswerOption> AnswerOptions => _answerOptions.AsReadOnly();

    private TheoryQuestion() { }

    public TheoryQuestion(string questionText, int lessonId)
    {
        if (string.IsNullOrWhiteSpace(questionText))
            throw new ArgumentException("Текст питання не може бути порожнім.");

        QuestionText = questionText.Trim();
        LessonId = lessonId;
    }

    public void AddAnswerOption(string text, bool isCorrect)
    {
        if (_answerOptions.Count >= MaxAnswers)
            throw new InvalidOperationException(
                $"Питання не може мати більше {MaxAnswers} відповідей."
            );

        var option = new AnswerOption(text, isCorrect);

        _answerOptions.Add(option);
    }

    public void RemoveAnswerOption(int answerId)
    {
        var option = _answerOptions.FirstOrDefault(a => a.Id == answerId);

        if (option == null)
            throw new InvalidOperationException("Відповідь не знайдено.");

        _answerOptions.Remove(option);

        Validate();
    }

    public void Validate()
    {
        if (_answerOptions.Count < MinAnswers)
            throw new InvalidOperationException(
                $"Питання повинно мати мінімум {MinAnswers} відповіді."
            );

        if (_answerOptions.Count > MaxAnswers)
            throw new InvalidOperationException(
                $"Питання не може мати більше {MaxAnswers} відповідей."
            );

        var correctCount = _answerOptions.Count(a => a.IsCorrect);
        var incorrectCount = _answerOptions.Count - correctCount;

        if (correctCount < MinCorrectAnswers)
            throw new InvalidOperationException(
                $"Має бути мінімум {MinCorrectAnswers} правильна відповідь."
            );

        if (correctCount > MaxCorrectAnswers)
            throw new InvalidOperationException(
                $"Не більше {MaxCorrectAnswers} правильних відповідей."
            );

        if (incorrectCount < 1)
            throw new InvalidOperationException(
                "Має бути хоча б одна неправильна відповідь."
            );
    }
}