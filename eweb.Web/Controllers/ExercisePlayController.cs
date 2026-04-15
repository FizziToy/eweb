using eweb.Domain.Entities.Attempts;
using eweb.Domain.Entities.Progress;
using eweb.Infrastructure.Data;
using eweb.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using eweb.Web.Models.ExercisePlay;
using System.Text.Json;

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
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);

        var exercises = await _context.InteractiveExercises
            .Where(x => x.IsPublished)
            .ToListAsync();

        var progresses = await _context.UserExerciseProgresses
            .Where(x => x.UserId == userId)
            .ToListAsync();

        var completedIds = progresses
            .Where(x => x.IsFullyCompleted)
            .Select(x => x.ExerciseId)
            .ToHashSet();

        ViewBag.CompletedIds = completedIds;

        return View(exercises);
    }
    // START

    [HttpPost]
    public async Task<IActionResult> Start(int exerciseId)
    {
        var userId = _userManager.GetUserId(User);

        //  СПОЧАТКУ перевіряємо чи є незавершена спроба
        var existingAttempt = await _context.ExerciseAttempts
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.ExerciseId == exerciseId &&
                !x.IsFinished);

        if (existingAttempt != null)
        {
            return RedirectToAction("Run", new { attemptId = existingAttempt.Id });
        }

        //  рахуємо тільки завершені
        var existingCount = await _context.ExerciseAttempts
            .CountAsync(x =>
                x.UserId == userId &&
                x.ExerciseId == exerciseId &&
                x.IsFinished);

        var progress = await _context.UserExerciseProgresses
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.ExerciseId == exerciseId);

        var allowedAttempts = progress?.GetTotalAllowedAttempts() ?? 10;

        // тепер створюємо нову спробу
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

        return Json(new
        {
            correct = correctCount,
            total = allTaskIds.Count,
            isFully
        });
    }

    //SUBMITTASK

    [HttpPost]
    public async Task<IActionResult> SubmitTask(
    int attemptId,
    int taskId,
    List<int> selectedIndexes,
    string selectedOrder,
    string selectedPairs)
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

        var task = await _context.ExerciseTasks
            .FirstOrDefaultAsync(x => x.Id == taskId);

        if (task == null)
            return NotFound();

        selectedIndexes ??= new List<int>();

        // MULTIPLE CHOICE
        if (task.Type.ToString() == "MultipleChoice")
        {
            var data = JsonSerializer.Deserialize<MultipleChoiceData>(
                task.DataJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (data == null || data.Options == null)
                return BadRequest("Invalid task data");

            var correctIndexes = data.Options
                .Select((opt, index) => new { opt, index })
                .Where(x => x.opt.IsCorrect)
                .Select(x => x.index)
                .OrderBy(x => x)
                .ToList();

            var selected = selectedIndexes.OrderBy(x => x).ToList();

            var isCorrect = correctIndexes.SequenceEqual(selected);

            return await SaveAttempt(attempt, taskId, isCorrect, userId);
        }

        // REORDER
        if (task.Type.ToString() == "Reorder")
        {
            var data = JsonSerializer.Deserialize<ReorderData>(
                task.DataJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (data == null || string.IsNullOrEmpty(data.CorrectOrder))
                return BadRequest("Invalid reorder data");

            if (string.IsNullOrEmpty(selectedOrder))
                return BadRequest("Order not provided");

            // 🔥 ТІ САМІ ПЕРЕВІРКИ ЯК В SaveAttempt
            var attemptsForTask = attempt.TaskAttempts
                .Where(x => x.ExerciseTaskId == taskId);

            var attemptsCount = attemptsForTask.Count();
            var alreadyCorrect = attemptsForTask.Any(x => x.IsCorrect);

            if (alreadyCorrect)
                return BadRequest("Вже правильно вирішено");

            if (attemptsCount >= 2)
                return BadRequest("Спроби вичерпано");

            // логіка reorder
            var correctList = data.CorrectOrder
                .Split(',')
                .Select(int.Parse)
                .ToList();

            var selectedList = selectedOrder
                .Split(',')
                .Select(int.Parse)
                .ToList();

            var isCorrect = selectedList.SequenceEqual(correctList);

            // запис результату
            attempt.RegisterTaskAttempt(taskId, isCorrect);

            await _context.SaveChangesAsync();

            return Json(new
            {
                isCorrect,
                correctOrder = data.CorrectOrder
            });
        }

        // FILL GAPS (поки як single choice)
        if (task.Type.ToString() == "FillGaps")
        {
            var data = JsonSerializer.Deserialize<FillGapsData>(
                task.DataJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (data == null)
                return BadRequest("Invalid fill data");

            var selected = selectedIndexes.FirstOrDefault();

            var isCorrect = selected == data.CorrectOptionIndex;

            return await SaveAttempt(attempt, taskId, isCorrect, userId);
        }

        // MATCH PAIRS
        if (task.Type.ToString() == "MatchPairs")
        {
            var data = JsonSerializer.Deserialize<MatchPairsData>(
                task.DataJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (data == null)
                return BadRequest("Invalid match data");

            if (string.IsNullOrEmpty(selectedPairs))
                return BadRequest("Pairs not provided");

            var userPairs = JsonSerializer.Deserialize<List<UserPair>>(selectedPairs);

            if (userPairs == null || userPairs.Count != data.Pairs.Count)
                return await SaveAttempt(attempt, taskId, false, userId);

            var usedRightValues = new HashSet<string>();

            var isCorrect = true;

            foreach (var pair in userPairs)
            {
                var leftIndex = int.Parse(pair.LeftIndex);

                // перевірка індексу
                if (leftIndex < 0 || leftIndex >= data.Pairs.Count)
                {
                    isCorrect = false;
                    break;
                }

                var correctRight = data.Pairs[leftIndex].Right;

                // ❗ головна перевірка
                if (correctRight != pair.RightValue)
                {
                    isCorrect = false;
                    break;
                }

                // ❗ перевірка дублювання
                if (!usedRightValues.Add(pair.RightValue))
                {
                    isCorrect = false;
                    break;
                }
            }

            return await SaveAttempt(attempt, taskId, isCorrect, userId);
        }

        await _context.SaveChangesAsync();

        return BadRequest("Невідомий тип задачі");

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
                    DataJson = t.DataJson,
                    Type = t.Type.ToString()
                })
                .ToList()
        };

        return View(model);
    }

    private async Task<IActionResult> SaveAttempt(
    ExerciseAttempt attempt,
    int taskId,
    bool isCorrect,
    string userId)
    {
        var attemptsForTask = attempt.TaskAttempts
            .Where(x => x.ExerciseTaskId == taskId);

        var attemptsCount = attemptsForTask.Count();
        var alreadyCorrect = attemptsForTask.Any(x => x.IsCorrect);

        if (alreadyCorrect)
            return BadRequest("Вже правильно вирішено");

        if (attemptsCount >= 2)
            return BadRequest("Спроби вичерпано");

        attempt.RegisterTaskAttempt(taskId, isCorrect);

        if (isCorrect)
        {
            var exists = await _context.UserExerciseTaskProgresses
                .AnyAsync(x =>
                    x.UserId == userId &&
                    x.ExerciseTaskId == taskId);

            if (!exists)
            {
                _context.UserExerciseTaskProgresses
                    .Add(new UserExerciseTaskProgress(userId, taskId));
            }
        }

        await _context.SaveChangesAsync();

        return Json(new { isCorrect });
    }

}