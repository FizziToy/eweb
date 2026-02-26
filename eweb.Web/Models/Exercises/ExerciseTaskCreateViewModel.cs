using eweb.Domain.Entities.Exercises;
using System.ComponentModel.DataAnnotations;

namespace eweb.Web.Models.Exercises;

public class ExerciseTaskCreateViewModel
{
    [Required]
    public ExerciseType Type { get; set; }

    [Required]
    public string QuestionText { get; set; } = null!;

    public int StarsReward { get; set; } = 1;

    public int Order { get; set; }

    // =========================
    // MultipleChoice (4 варіанти)
    // =========================
    public string? Option1 { get; set; }
    public string? Option2 { get; set; }
    public string? Option3 { get; set; }
    public string? Option4 { get; set; }

    public bool IsOption1Correct { get; set; }
    public bool IsOption2Correct { get; set; }
    public bool IsOption3Correct { get; set; }
    public bool IsOption4Correct { get; set; }

    // =========================
    // Reorder (4 елементи)
    // =========================
    public string? ReorderItem1 { get; set; }
    public string? ReorderItem2 { get; set; }
    public string? ReorderItem3 { get; set; }
    public string? ReorderItem4 { get; set; }
    public string? CorrectOrder { get; set; }

    // =========================
    // MatchPairs (4 пари)
    // =========================
    public string? Left1 { get; set; }
    public string? Right1 { get; set; }

    public string? Left2 { get; set; }
    public string? Right2 { get; set; }

    public string? Left3 { get; set; }
    public string? Right3 { get; set; }

    public string? Left4 { get; set; }
    public string? Right4 { get; set; }
}