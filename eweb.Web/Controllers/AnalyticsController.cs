using eweb.Domain.Constants;
using eweb.Domain.Services;
using eweb.Infrastructure.Data;
using eweb.Web.Models.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eweb.Web.Controllers;

[Authorize(Roles = RoleNames.Admin)]
public class AnalyticsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IProgressCalculator _progressCalculator;

    public AnalyticsController(
        ApplicationDbContext context,
        IProgressCalculator progressCalculator)
    {
        _context = context;
        _progressCalculator = progressCalculator;
    }

    public async Task<IActionResult> Index()
    {
        var model = new AnalyticsViewModel();

        var dailyRaw = await _context.LessonTestAttempts
            .Where(a => a.IsFinished)
            .GroupBy(a => a.FinishedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                AverageResult = g.Average(a => (double)a.ResultPercent),
                AverageTimeSeconds = g.Average(a =>
                    EF.Functions.DateDiffSecond(a.StartedAt, a.FinishedAt)),
                Attempts = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        model.DailyStats = dailyRaw
            .Select(x => new DailySuccessStat
            {
                Date = DateOnly.FromDateTime(x.Date),
                AverageResult = x.AverageResult,
                AverageTimeSeconds = x.AverageTimeSeconds,
                Attempts = x.Attempts
            })
            .ToList();

        model.CategoryStats = await _context.LessonCategories
            .Select(c => new CategoryStat
            {
                CategoryName = c.Name,

                TotalAnswers = _context.LessonTestAttempts
                    .Count(a =>
                        a.Lesson.CategoryId == c.Id &&
                        a.IsFinished),

                CorrectAnswers = _context.LessonTestAttempts
                    .Count(a =>
                        a.Lesson.CategoryId == c.Id &&
                        a.IsFinished &&
                        a.ResultPercent >= 50),

                AverageResult = _context.LessonTestAttempts
                    .Where(a =>
                        a.Lesson.CategoryId == c.Id &&
                        a.IsFinished)
                    .Average(a => (double?)a.ResultPercent) ?? 0,

                AverageTimeSeconds = _context.LessonTestAttempts
                    .Where(a =>
                        a.Lesson.CategoryId == c.Id &&
                        a.IsFinished)
                    .Average(a => (double?)EF.Functions.DateDiffSecond(a.StartedAt, a.FinishedAt)) ?? 0
            })
            .ToListAsync();

        model.OverallSuccess = model.CategoryStats.Any()
            ? model.CategoryStats.Average(c => c.SuccessPercent)
            : 0;

        foreach (var stat in model.CategoryStats)
        {
            var timePenalty = stat.AverageTimeSeconds > 0 ? stat.AverageTimeSeconds * 0.02 : 0;
            stat.Score = stat.SuccessPercent - timePenalty;
        }

        var categoriesWithAttempts = model.CategoryStats
            .Where(s => s.TotalAnswers > 0)
            .ToList();

        if (categoriesWithAttempts.Any())
        {
            var allPerfect = categoriesWithAttempts
                .All(s => s.SuccessPercent == 100);

            if (!allPerfect)
            {
                var minScore = categoriesWithAttempts.Min(s => s.Score);

                model.WeakestCategories = categoriesWithAttempts
                    .Where(s => s.Score == minScore)
                    .Select(s => s.CategoryName)
                    .ToList();
            }
        }

        return View(model);
    }

    public async Task<IActionResult> Students()
    {
        var totalLessons = await _context.Lessons
            .Where(l => l.IsPublished)
            .CountAsync();

        var totalQuestions = await _context.TheoryQuestions
            .Where(q => q.Lesson.IsPublished)
            .CountAsync();

        var totalTasks = await _context.ExerciseTasks
            .Join(
                _context.InteractiveExercises.Where(e => e.IsPublished),
                task => task.ExerciseId,
                exercise => exercise.Id,
                (task, exercise) => task)
            .CountAsync();

        var adminUserIds = await _context.UserRoles
            .Join(
                _context.Roles.Where(r => r.Name == RoleNames.Admin),
                userRole => userRole.RoleId,
                role => role.Id,
                (userRole, role) => userRole.UserId)
            .ToListAsync();

        var students = await _context.Users
            .Where(u => !adminUserIds.Contains(u.Id))
            .OrderBy(u => u.Email)
            .Select(u => new
            {
                u.Id,
                Email = u.Email ?? u.UserName ?? "Без email"
            })
            .ToListAsync();

        var studentIds = students.Select(s => s.Id).ToList();

        var openedLessonsByUser = await _context.UserLessonProgresses
            .Where(p => studentIds.Contains(p.UserId))
            .Join(
                _context.Lessons.Where(l => l.IsPublished),
                progress => progress.LessonId,
                lesson => lesson.Id,
                (progress, lesson) => progress)
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => Math.Min(x.Count, totalLessons));

        var completedQuestionsByUser = await _context.UserQuestionProgresses
            .Where(p => studentIds.Contains(p.UserId))
            .Join(
                _context.TheoryQuestions.Where(q => q.Lesson.IsPublished),
                progress => progress.QuestionId,
                question => question.Id,
                (progress, question) => progress)
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => Math.Min(x.Count, totalQuestions));

        var completedTasksByUser = await _context.UserExerciseTaskProgresses
            .Where(p => studentIds.Contains(p.UserId))
            .Join(
                _context.ExerciseTasks,
                progress => progress.ExerciseTaskId,
                task => task.Id,
                (progress, task) => new { Progress = progress, Task = task })
            .Join(
                _context.InteractiveExercises.Where(e => e.IsPublished),
                x => x.Task.ExerciseId,
                exercise => exercise.Id,
                (x, exercise) => x.Progress)
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => Math.Min(x.Count, totalTasks));

        var model = new StudentProgressViewModel();

        foreach (var student in students)
        {
            var openLessons = openedLessonsByUser.GetValueOrDefault(student.Id);
            var completedQuestions = completedQuestionsByUser.GetValueOrDefault(student.Id);
            var completedTasks = completedTasksByUser.GetValueOrDefault(student.Id);

            model.Students.Add(new StudentProgressRow
            {
                UserId = student.Id,
                Email = student.Email,
                OpenLessons = openLessons,
                TotalLessons = totalLessons,
                CompletedQuestions = completedQuestions,
                TotalQuestions = totalQuestions,
                CompletedTasks = completedTasks,
                TotalTasks = totalTasks,
                ProgressPercent = _progressCalculator.Calculate(
                    openLessons,
                    totalLessons,
                    completedQuestions,
                    totalQuestions,
                    completedTasks,
                    totalTasks)
            });
        }

        model.Students = model.Students
            .OrderByDescending(s => s.ProgressPercent)
            .ThenBy(s => s.Email)
            .ToList();

        return View(model);
    }
}
