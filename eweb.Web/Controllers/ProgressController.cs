using eweb.Domain.Services;
using eweb.Infrastructure.Data;
using eweb.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eweb.Web.Controllers;

[Authorize]
public class ProgressController(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    IProgressCalculator progressCalculator) : Controller
{
    private readonly ApplicationDbContext _context = context;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IProgressCalculator _progressCalculator = progressCalculator;

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);

        var openedLessons = await _context.UserLessonProgresses
            .CountAsync(x => x.UserId == userId);

        var totalLessons = await _context.Lessons
            .Where(l => l.IsPublished)
            .CountAsync();

        var completedQuestions = await _context.UserQuestionProgresses
            .CountAsync(x => x.UserId == userId);

        var totalQuestions = await _context.TheoryQuestions
            .CountAsync();

        // Поки вправ нема
        int completedTasks = 0;
        int totalTasks = 0;

        var progress = _progressCalculator.Calculate(
            openedLessons,
            totalLessons,
            completedQuestions,
            totalQuestions,
            completedTasks,
            totalTasks
        );

        ViewBag.Progress = progress;

        return View();
    }
}