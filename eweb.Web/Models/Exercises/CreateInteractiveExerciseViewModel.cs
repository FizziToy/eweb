using System.ComponentModel.DataAnnotations;

namespace eweb.Web.Models.Exercises;

public class CreateInteractiveExerciseViewModel
{
    [Required]
    public int LessonId { get; set; }

    [Required]
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public List<ExerciseTaskCreateViewModel> Tasks { get; set; } = new();
}