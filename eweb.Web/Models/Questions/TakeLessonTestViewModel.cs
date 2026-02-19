namespace eweb.Web.Models.Questions;

public class TakeLessonTestViewModel
{
    public int LessonId { get; set; }

    public string LessonTitle { get; set; } = string.Empty;

    public List<QuestionForTestViewModel> Questions { get; set; } = new();
}

public class QuestionForTestViewModel
{
    public int QuestionId { get; set; }

    public string QuestionText { get; set; } = string.Empty;

    public List<AnswerForTestViewModel> Answers { get; set; } = new();
}

public class AnswerForTestViewModel
{
    public int AnswerId { get; set; }

    public string Text { get; set; } = string.Empty;

    public bool IsSelected { get; set; }
}