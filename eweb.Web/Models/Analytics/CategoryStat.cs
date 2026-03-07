namespace eweb.Web.Models.Analytics;

public class CategoryStat
{
    public string CategoryName { get; set; } = string.Empty;

    public int TotalAnswers { get; set; }

    public int CorrectAnswers { get; set; }

    public double SuccessPercent =>
        TotalAnswers == 0 ? 0 :
        (double)CorrectAnswers / TotalAnswers * 100;
}