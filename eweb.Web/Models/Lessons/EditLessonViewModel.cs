using Microsoft.AspNetCore.Mvc.Rendering;

namespace eweb.Web.Models.Lessons;

public class EditLessonViewModel
{
    public int Id { get; set; }

    public int Number { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
    public int CategoryId { get; set; }

    public bool IsPublished { get; set; }

    public bool IsActuallyPublished { get; set; }

    public int MaxNumber { get; set; }

    public List<QuestionEditModel> Questions { get; set; } = new();
    public IEnumerable<SelectListItem>? Categories { get; set; }
}

public class QuestionEditModel
{
    public int Id { get; set; }

    public string QuestionText { get; set; } = string.Empty;

    public List<AnswerEditModel> Answers { get; set; } = new();
}

public class AnswerEditModel
{
    public int Id { get; set; }

    public string Text { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }
}