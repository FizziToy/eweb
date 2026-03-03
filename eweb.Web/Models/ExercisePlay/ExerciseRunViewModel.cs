namespace eweb.Web.Models.ExercisePlay
{
    public class ExerciseRunViewModel
    {
        public int AttemptId { get; set; }
        public string ExerciseTitle { get; set; } = null!;
        public bool IsFinished { get; set; }
        public List<ExerciseTaskViewModel> Tasks { get; set; } = new();
    }
}
