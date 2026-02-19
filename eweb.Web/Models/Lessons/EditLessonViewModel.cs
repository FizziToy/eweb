namespace eweb.Web.Models.Lessons;

public class EditLessonViewModel
{
    public int Id { get; set; }

    public int Number { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public bool IsPublished { get; set; }
}