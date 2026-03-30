using eweb.Domain.Constants;
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

    public AnalyticsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var model = new AnalyticsViewModel();

        // Беремо сирі дані
        var dailyRaw = await _context.LessonTestAttempts
            .Where(a => a.IsFinished)
            .Select(a => new
            {
                a.FinishedAt,
                a.StartedAt,
                a.ResultPercent
            })
            .ToListAsync();

        // Групування по днях
        model.DailyStats = dailyRaw
            .GroupBy(a => a.FinishedAt.Date)
            .Select(g => new DailySuccessStat
            {
                Date = DateOnly.FromDateTime(g.Key),

                AverageResult = g.Average(a => (double)a.ResultPercent),

                AverageTimeSeconds = g.Average(a =>
                    (a.FinishedAt - a.StartedAt).TotalSeconds),

                Attempts = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToList();

        // Статистика по категоріях
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
                    .Average(a => (double?)a.ResultPercent) ?? 0
            })
            .ToListAsync();

        // Загальна успішність
        model.OverallSuccess = model.CategoryStats.Any()
            ? model.CategoryStats.Average(c => c.SuccessPercent)
            : 0;

        // Score категорій
        var avgTime = model.DailyStats.Any()
            ? model.DailyStats.Average(d => d.AverageTimeSeconds)
            : 0;

        foreach (var stat in model.CategoryStats)
        {
            var timePenalty = avgTime > 0 ? avgTime * 0.02 : 0;
            stat.Score = stat.SuccessPercent - timePenalty;
        }

        // Слабкі теми
        if (model.CategoryStats.Any())
        {
            var allPerfect = model.CategoryStats
                .All(s => s.SuccessPercent == 100);

            if (!allPerfect)
            {
                var minScore = model.CategoryStats.Min(s => s.Score);

                model.WeakestCategories = model.CategoryStats
                    .Where(s => s.Score == minScore)
                    .Select(s => s.CategoryName)
                    .ToList();
            }
        }

        return View(model);
    }
}