using eweb.Web.Models.Exercises;

namespace eweb.Web.Models.Exercises;

public class EditInteractiveExerciseViewModel
{
    public int Id { get; set; }

    public int LessonId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public List<ExerciseTaskEditViewModel> Tasks { get; set; } = new();
}