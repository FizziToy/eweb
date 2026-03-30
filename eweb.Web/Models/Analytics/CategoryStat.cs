namespace eweb.Web.Models.Analytics;

public class CategoryStat
{
    public string CategoryName { get; set; } = string.Empty;

    public int TotalAnswers { get; set; }

    public int CorrectAnswers { get; set; }

    public double AverageResult { get; set; }

    public double SuccessPercent => AverageResult;

    public double Score { get; set; }
}