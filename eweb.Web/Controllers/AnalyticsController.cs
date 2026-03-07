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

        model.DailyStats = _context.LessonTestAttempts
             .Where(a => a.IsFinished)
             .AsEnumerable()
             .GroupBy(a => DateOnly.FromDateTime(a.FinishedAt))
             .Select(g => new DailySuccessStat
             {
                 Date = g.Key,

                 AverageResult = g.Average(a => a.ResultPercent),

                 AverageTimeSeconds = g.Average(a =>
                     (a.FinishedAt - a.StartedAt).TotalSeconds),

                 Attempts = g.Count()
             })
             .OrderBy(x => x.Date)
             .ToList();

        model.CategoryStats = await _context.LessonCategories
            .Select(c => new CategoryStat
            {
                CategoryName = c.Name,

                TotalAnswers = _context.UserQuestionProgresses
                    .Count(p => p.Question.Lesson.Category.Id == c.Id),

                CorrectAnswers = _context.UserQuestionProgresses
                    .Count(p => p.Question.Lesson.Category.Id == c.Id)
            })
            .ToListAsync();

        if (model.CategoryStats.Any())
        {
            var weakest = model.CategoryStats
                .OrderBy(s => s.TotalAnswers)
                .First();

            model.WeakestCategory = weakest.CategoryName;
        }

        return View(model);
    }
}