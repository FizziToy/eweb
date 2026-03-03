namespace eweb.Domain.Services;

public class ProgressCalculator : IProgressCalculator
{
    private const double LessonWeight = 10;
    private const double QuestionWeight = 35;
    private const double TaskWeight = 55;

    public double Calculate(
        int openedLessons,
        int totalLessons,
        int completedQuestions,
        int totalQuestions,
        int completedTasks,
        int totalTasks)
    {
        double lessonPart = totalLessons == 0
            ? 0
            : ((double)openedLessons / totalLessons) * LessonWeight;

        double questionPart = totalQuestions == 0
            ? 0
            : ((double)completedQuestions / totalQuestions) * QuestionWeight;

        double taskPart = totalTasks == 0
            ? 0
            : ((double)completedTasks / totalTasks) * TaskWeight;

        var result = lessonPart + questionPart + taskPart;

        result = Math.Max(0, result);
        result = Math.Min(100, result);

        return Math.Round(result, 2);
    }
}