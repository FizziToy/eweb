namespace eweb.Web.Models.Home
{
    public class HomeViewModel
    {
        public int OpenLessons { get; set; }
        public int TotalLessons { get; set; }

        public int ExercisesSolved { get; set; }

        public int StarsEarned { get; set; }
        public int StarsTotal { get; set; }

        public double ProgressPercent { get; set; }

        public ContinueLessonViewModel? LastViewedLesson { get; set; }
        public ContinueExerciseViewModel? LastOpenedExercise { get; set; }
    }

    public class ContinueLessonViewModel
    {
        public int LessonId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int ProgressPercent { get; set; }
    }

    public class ContinueExerciseViewModel
    {
        public int AttemptId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int ProgressPercent { get; set; }
        public bool IsFinished { get; set; }
    }
}
