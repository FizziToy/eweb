namespace eweb.Domain.Entities;

public class AnswerOption
{
    public int Id { get; private set; }

    public string Text { get; private set; } = null!;

    public bool IsCorrect { get; private set; }

    public int TheoryQuestionId { get; private set; }

    private AnswerOption() { }

    public AnswerOption(string text, bool isCorrect)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Текст відповіді не може бути порожнім.", nameof(text));

        Text = text.Trim();
        IsCorrect = isCorrect;
    }
}