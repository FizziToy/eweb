namespace eweb.Web.Models.Analytics;

public class StudentProgressViewModel
{
    public List<StudentProgressRow> Students { get; set; } = new();
}

public class StudentProgressRow
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int OpenLessons { get; set; }
    public int TotalLessons { get; set; }
    public int CompletedQuestions { get; set; }
    public int TotalQuestions { get; set; }
    public int CompletedTasks { get; set; }
    public int TotalTasks { get; set; }
    public double ProgressPercent { get; set; }
}
