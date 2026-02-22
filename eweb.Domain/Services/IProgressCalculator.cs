namespace eweb.Domain.Services;

public interface IProgressCalculator
{
    double Calculate(
        int openedLessons,
        int totalLessons,
        int completedQuestions,
        int totalQuestions,
        int completedTasks,
        int totalTasks);
}