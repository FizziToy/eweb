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
    }
}