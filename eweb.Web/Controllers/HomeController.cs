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

            completedQuestions = Math.Min(completedQuestions, totalQuestions);

            var totalTasks = await _context.ExerciseTasks
                .Join(
                    _context.InteractiveExercises.Where(e => e.IsPublished),
                    task => task.ExerciseId,
                    exercise => exercise.Id,
                    (task, exercise) => task
                )
                .CountAsync();

            var completedTasks = await _context.UserExerciseTaskProgresses
                .Where(p => p.UserId == userId)
                .Join(
                    _context.ExerciseTasks,
                    progress => progress.ExerciseTaskId,
                    task => task.Id,
                    (progress, task) => new { Progress = progress, Task = task }
                )
                .Join(
                    _context.InteractiveExercises.Where(e => e.IsPublished),
                    x => x.Task.ExerciseId,
                    exercise => exercise.Id,
                    (x, exercise) => x.Task
                )
                .CountAsync();

            completedTasks = Math.Min(completedTasks, totalTasks);

            var totalStars = await _context.ExerciseTasks
                .Join(
                    _context.InteractiveExercises.Where(e => e.IsPublished),
                    task => task.ExerciseId,
                    exercise => exercise.Id,
                    (task, exercise) => task.StarsReward
                )
                .SumAsync();

            var earnedStars = await _context.UserExerciseTaskProgresses
                .Where(p => p.UserId == userId)
                .Join(
                    _context.ExerciseTasks,
                    progress => progress.ExerciseTaskId,
                    task => task.Id,
                    (progress, task) => new { Progress = progress, Task = task }
                )
                .Join(
                    _context.InteractiveExercises.Where(e => e.IsPublished),
                    x => x.Task.ExerciseId,
                    exercise => exercise.Id,
                    (x, exercise) => x.Task.StarsReward
                )
                .SumAsync();

            earnedStars = Math.Min(earnedStars, totalStars);

            var progress = _progressCalculator.Calculate(
                openedLessons,
                totalLessons,
                completedQuestions,
                totalQuestions,
                completedTasks,
                totalTasks
            );

            ContinueLessonViewModel? lastViewedLesson = null;
            ContinueExerciseViewModel? lastOpenedExercise = null;

            if (userId != null)
            {
                lastViewedLesson = await _context.UserLessonProgresses
                    .Where(p => p.UserId == userId)
                    .Join(
                        _context.Lessons.Where(l => l.IsPublished),
                        p => p.LessonId,
                        l => l.Id,
                        (p, l) => new { Progress = p, Lesson = l }
                    )
                    .OrderByDescending(x => x.Progress.OpenedAt)
                    .Select(x => new ContinueLessonViewModel
                    {
                        LessonId = x.Lesson.Id,
                        Title = x.Lesson.Title,
                        Description = x.Lesson.Description,
                        ProgressPercent = 100
                    })
                    .FirstOrDefaultAsync();

                var lastOpenedExerciseData = await _context.ExerciseAttempts
                    .Where(a => a.UserId == userId)
                    .Join(
                        _context.InteractiveExercises.Where(e => e.IsPublished),
                        a => a.ExerciseId,
                        e => e.Id,
                        (a, e) => new
                        {
                            AttemptId = a.Id,
                            a.StartedAt,
                            a.IsFinished,
                            ExerciseId = e.Id,
                            e.Title,
                            e.Description
                        }
                    )
                    .OrderByDescending(x => x.StartedAt)
                    .FirstOrDefaultAsync();

                if (lastOpenedExerciseData != null)
                {
                    var exerciseTasksCount = await _context.ExerciseTasks
                        .CountAsync(t => t.ExerciseId == lastOpenedExerciseData.ExerciseId);

                    var correctTasksCount = await _context.TaskAttempts
                        .Where(t => t.ExerciseAttemptId == lastOpenedExerciseData.AttemptId && t.IsCorrect)
                        .Select(t => t.ExerciseTaskId)
                        .Distinct()
                        .CountAsync();

                    lastOpenedExercise = new ContinueExerciseViewModel
                    {
                        AttemptId = lastOpenedExerciseData.AttemptId,
                        Title = lastOpenedExerciseData.Title,
                        Description = lastOpenedExerciseData.Description ?? "Продовжіть виконання інтерактивної вправи",
                        ProgressPercent = exerciseTasksCount == 0
                            ? 0
                            : (int)Math.Round((double)correctTasksCount * 100 / exerciseTasksCount),
                        IsFinished = lastOpenedExerciseData.IsFinished
                    };
                }
            }

            var model = new HomeViewModel
            {
                OpenLessons = openedLessons,
                TotalLessons = totalLessons,
                ExercisesSolved = completedTasks,
                StarsEarned = earnedStars,
                StarsTotal = totalStars,
                ProgressPercent = progress,
                LastViewedLesson = lastViewedLesson,
                LastOpenedExercise = lastOpenedExercise
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
