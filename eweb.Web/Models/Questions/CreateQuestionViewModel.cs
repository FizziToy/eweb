namespace eweb.Web.Models.Questions;

public class CreateQuestionViewModel
{
    public int LessonId { get; set; }

    public string QuestionText { get; set; } = string.Empty;

    public List<AnswerOptionInputModel> Answers { get; set; } = new();
}

public class AnswerOptionInputModel
{
    public string Text { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }
}