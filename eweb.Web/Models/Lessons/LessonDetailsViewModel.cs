namespace eweb.Web.Models.Lessons;

public class LessonDetailsViewModel
{
    public int LessonId { get; set; }

    public int Number { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public bool IsPublished { get; set; }

    public DateOnly CreatedAt { get; set; }

    public double? ResultPercent { get; set; }

    public int AttemptsCount { get; set; }

    public bool ShowCorrectAnswers { get; set; }

    public bool AttemptsExceeded { get; set; }

    public bool ShowCorrectAnswersModal { get; set; }

    public List<QuestionVm> Questions { get; set; } = new();

    public class QuestionVm
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public List<AnswerVm> Answers { get; set; } = new();
    }

    public class AnswerVm
    {
        public int AnswerId { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }
}