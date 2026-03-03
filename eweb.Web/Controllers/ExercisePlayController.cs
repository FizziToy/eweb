using eweb.Domain.Entities.Attempts;
using eweb.Domain.Entities.Progress;
using eweb.Infrastructure.Data;
using eweb.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eweb.Web.Models.ExercisePlay;

[Authorize]
public class ExercisePlayController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ExercisePlayController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var exercises = await _context.InteractiveExercises
            .Where(x => x.IsPublished)
            .ToListAsync();

        return View(exercises);
    }

    // START

    [HttpPost]
    public async Task<IActionResult> Start(int exerciseId)
    {
        var userId = _userManager.GetUserId(User);

        var existingCount = await _context.ExerciseAttempts
            .CountAsync(x =>
                x.UserId == userId &&
                x.ExerciseId == exerciseId);

        var progress = await _context.UserExerciseProgresses
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.ExerciseId == exerciseId);

        var allowedAttempts = progress?.GetTotalAllowedAttempts() ?? 10;

        var attempt = ExerciseAttempt.Create(
            userId,
            exerciseId,
            existingCount,
            allowedAttempts);

        _context.ExerciseAttempts.Add(attempt);
        await _context.SaveChangesAsync();

        return RedirectToAction("Run", new { attemptId = attempt.Id });
    }

    // FINISH

    [HttpPost]
    public async Task<IActionResult> Finish(int attemptId)
    {
        var userId = _userManager.GetUserId(User);

        var attempt = await _context.ExerciseAttempts
            .Include(x => x.TaskAttempts)
            .FirstOrDefaultAsync(x =>
                x.Id == attemptId &&
                x.UserId == userId);

        if (attempt == null)
            return NotFound();

        if (attempt.IsFinished)
            return BadRequest();

        attempt.Finish();

        var correctCount = attempt.GetCorrectTasksCount();

        var allTaskIds = await _context.ExerciseTasks
            .Where(x => x.ExerciseId == attempt.ExerciseId)
            .Select(x => x.Id)
            .ToListAsync();

        var isFully = attempt.IsFullyCompleted(allTaskIds);

        var progress = await _context.UserExerciseProgresses
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.ExerciseId == attempt.ExerciseId);

        if (progress == null)
        {
            progress = new UserExerciseProgress(userId, attempt.ExerciseId);
            _context.UserExerciseProgresses.Add(progress);
        }

        progress.UpdateFromAttempt(correctCount, isFully);

        await _context.SaveChangesAsync();

        return RedirectToAction("Result", new { attemptId });
    }

    //SUBMITTASK

    [HttpPost]
    public async Task<IActionResult> SubmitTask(
    int attemptId,
    int taskId,
    bool isCorrect)
    {
        var userId = _userManager.GetUserId(User);

        var attempt = await _context.ExerciseAttempts
            .Include(x => x.TaskAttempts)
            .FirstOrDefaultAsync(x =>
                x.Id == attemptId &&
                x.UserId == userId);

        if (attempt == null)
            return NotFound();

        if (attempt.IsFinished)
            return BadRequest("Запуск вже завершений.");

        attempt.RegisterTaskAttempt(taskId, isCorrect);

        if (isCorrect)
        {
            var taskProgressExists = await _context.UserExerciseTaskProgresses
                .AnyAsync(x =>
                    x.UserId == userId &&
                    x.ExerciseTaskId == taskId);

            if (!taskProgressExists)
            {
                var taskProgress = new UserExerciseTaskProgress(userId, taskId);
                _context.UserExerciseTaskProgresses.Add(taskProgress);
            }
        }

        await _context.SaveChangesAsync();

        return Ok();
    }


    [HttpGet]
    public async Task<IActionResult> Run(int attemptId)
    {
        var userId = _userManager.GetUserId(User);

        var attempt = await _context.ExerciseAttempts
            .FirstOrDefaultAsync(x =>
                x.Id == attemptId &&
                x.UserId == userId);

        if (attempt == null)
            return NotFound();

        var exercise = await _context.InteractiveExercises
            .Include(x => x.Tasks)
            .FirstOrDefaultAsync(x =>
                x.Id == attempt.ExerciseId &&
                x.IsPublished);

        if (exercise == null)
            return NotFound();

        var model = new ExerciseRunViewModel
        {
            AttemptId = attempt.Id,
            ExerciseTitle = exercise.Title,
            IsFinished = attempt.IsFinished,
            Tasks = exercise.Tasks
                .OrderBy(t => t.Order)
                .Select(t => new ExerciseTaskViewModel
                {
                    TaskId = t.Id,
                    QuestionText = t.QuestionText,
                    DataJson = t.DataJson
                })
                .ToList()
        };

        return View(model);
    }
}