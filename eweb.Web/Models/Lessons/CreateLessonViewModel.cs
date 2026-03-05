namespace eweb.Web.Models.Lessons;

public class CreateLessonViewModel
{
    public int Number { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public bool IsPublished { get; set; }

    public List<QuestionInputModel> Questions { get; set; } = new();
}

public class QuestionInputModel
{
    public string QuestionText { get; set; } = string.Empty;

    public List<AnswerInputModel> Answers { get; set; } = new();
}

public class AnswerInputModel
{
    public string Text { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }
}