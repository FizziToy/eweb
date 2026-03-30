namespace eweb.Web.Models.Analytics;

public class AnalyticsViewModel
{
    public List<DailySuccessStat> DailyStats { get; set; } = new();

    public List<CategoryStat> CategoryStats { get; set; } = new();

    public List<string> WeakestCategories { get; set; } = new();

    public double OverallSuccess { get; set; }
}