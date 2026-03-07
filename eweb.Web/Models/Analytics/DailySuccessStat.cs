namespace eweb.Web.Models.Analytics;

public class DailySuccessStat
{
    public DateOnly Date { get; set; }

    public double AverageResult { get; set; }

    public double AverageTimeSeconds { get; set; }

    public int Attempts { get; set; }
}