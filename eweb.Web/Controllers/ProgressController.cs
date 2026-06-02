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