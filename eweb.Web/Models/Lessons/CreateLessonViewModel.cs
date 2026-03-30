using Microsoft.AspNetCore.Mvc.Rendering;

namespace eweb.Web.Models.Lessons;

public class CreateLessonViewModel
{
    public int Number { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public bool IsPublished { get; set; }

    public int MaxNumber { get; set; }
    public DateOnly CreatedAt { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public List<QuestionInputModel> Questions { get; set; } = new();
    public IEnumerable<SelectListItem>? Categories { get; set; }
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