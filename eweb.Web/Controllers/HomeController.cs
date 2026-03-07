using eweb.Domain.Services;
using eweb.Infrastructure.Data;
using eweb.Web.Models;
using eweb.Web.Models.Home;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace eweb.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IProgressCalculator _progressCalculator;

        public HomeController(
            ILogger<HomeController> logger,
            ApplicationDbContext context,
            IProgressCalculator progressCalculator)
        {
            _logger = logger;
            _context = context;
            _progressCalculator = progressCalculator;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var totalLessons = await _context.Lessons
                .Where(l => l.IsPublished)
                .CountAsync();

            var openedLessons = await _context.UserLessonProgresses
                .Where(p => p.UserId == userId)
                .Join(
                    _context.Lessons.Where(l => l.IsPublished),
                    p => p.LessonId,
                    l => l.Id,
                    (p, l) => p
                )
                .CountAsync();

            openedLessons = Math.Min(openedLessons, totalLessons);

            var totalQuestions = await _context.TheoryQuestions
                .Where(q => q.Lesson.IsPublished)
                .CountAsync();

            var completedQuestions = await _context.UserQuestionProgresses
                .Where(p => p.UserId == userId)
                .Join(
                    _context.TheoryQuestions.Where(q => q.Lesson.IsPublished),
                    p => p.QuestionId,
                    q => q.Id,
                    (p, q) => p
                )
                .CountAsync();

            var totalTasks = await _context.ExerciseTasks.CountAsync();

            var completedTasks = await _context.UserExerciseTaskProgresses
                .Where(p => p.UserId == userId)
                .CountAsync();

            var progress = _progressCalculator.Calculate(
                openedLessons,
                totalLessons,
                completedQuestions,
                totalQuestions,
                completedTasks,
                totalTasks
            );

            var model = new HomeViewModel
            {
                OpenLessons = openedLessons,
                TotalLessons = totalLessons,
                ExercisesSolved = completedTasks,
                StarsEarned = completedTasks,
                StarsTotal = totalTasks,
                ProgressPercent = progress
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}