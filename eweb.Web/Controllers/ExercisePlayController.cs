using eweb.Domain.Entities.Attempts;
using eweb.Domain.Entities.Exercises;
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
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

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

        if (userId == null)
            return Challenge();

        var exerciseExists = await _context.InteractiveExercises
            .AnyAsync(x => x.Id == exerciseId && x.IsPublished);

        if (!exerciseExists)
            return NotFound();

        var existingAttempt = await _context.ExerciseAttempts
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.ExerciseId == exerciseId &&
                !x.IsFinished);

        if (existingAttempt != null)
            return RedirectToAction("Run", new { attemptId = existingAttempt.Id });

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

        try
        {
            var attempt = ExerciseAttempt.Create(
                userId,
                exerciseId,
                existingCount,
                allowedAttempts);

            _context.ExerciseAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            return RedirectToAction("Run", new { attemptId = attempt.Id });
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction("Index");
        }
    }

    // FINISH

    [HttpPost]
    public async Task<IActionResult> Finish(int attemptId)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

        var attempt = await _context.ExerciseAttempts
            .Include(x => x.TaskAttempts)
            .FirstOrDefaultAsync(x =>
                x.Id == attemptId &&
                x.UserId == userId);

        if (attempt == null)
            return NotFound();

        if (attempt.IsFinished)
            return BadRequest();

        var allTaskIds = await _context.ExerciseTasks
            .Where(x => x.ExerciseId == attempt.ExerciseId)
            .Select(x => x.Id)
            .ToListAsync();

        if (!CanFinishAttempt(attempt, allTaskIds))
            return BadRequest("Перевірте всі завдання або використайте доступні спроби перед завершенням.");

        attempt.Finish();

        var correctCount = attempt.GetCorrectTasksCount();

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

        if (userId == null)
            return Challenge();

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
            .FirstOrDefaultAsync(x =>
                x.Id == taskId &&
                x.ExerciseId == attempt.ExerciseId);

        if (task == null)
            return NotFound();

        selectedIndexes ??= new List<int>();

        // MULTIPLE CHOICE
        if (task.Type == ExerciseType.MultipleChoice)
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
        if (task.Type == ExerciseType.Reorder)
        {
            var data = JsonSerializer.Deserialize<ReorderData>(
                task.DataJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (data == null || string.IsNullOrEmpty(data.CorrectOrder))
                return BadRequest("Invalid reorder data");

            if (string.IsNullOrEmpty(selectedOrder))
                return BadRequest("Order not provided");

            if (!TryParseOrder(data.CorrectOrder, out var correctList) ||
                !TryParseOrder(selectedOrder, out var selectedList))
            {
                return BadRequest("Порядок має містити числа 1,2,3,4 без повторів.");
            }

            if (!HasSameItems(selectedList, correctList))
                return BadRequest("Порядок містить неправильний набір елементів.");

            var isCorrect = selectedList.SequenceEqual(correctList);

            return await SaveAttempt(attempt, taskId, isCorrect, userId, new
            {
                correctOrder = data.CorrectOrder
            });
        }

        // FILL GAPS (поки як single choice)
        if (task.Type == ExerciseType.FillGaps)
        {
            var data = JsonSerializer.Deserialize<FillGapsData>(
                task.DataJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (data == null)
                return BadRequest("Invalid fill data");

            if (selectedIndexes.Count == 0)
                return BadRequest("Оберіть відповідь.");

            var selected = selectedIndexes[0];

            var isCorrect = selected == data.CorrectOptionIndex;

            return await SaveAttempt(attempt, taskId, isCorrect, userId);
        }

        // MATCH PAIRS
        if (task.Type == ExerciseType.MatchPairs)
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
                return await SaveMatchAttempt(attempt, taskId, false, userId, new List<object>());

            var usedLeftIndexes = new HashSet<int>();
            var usedRightValues = new HashSet<string>();

            var isCorrect = true;
            var pairResults = new List<object>();

            foreach (var pair in userPairs)
            {
                if (!int.TryParse(pair.LeftIndex, out var leftIndex))
                {
                    isCorrect = false;
                    pairResults.Add(new
                    {
                        pair.LeftIndex,
                        pair.RightValue,
                        isCorrect = false
                    });
                    continue;
                }

                if (leftIndex < 0 || leftIndex >= data.Pairs.Count)
                {
                    isCorrect = false;
                    pairResults.Add(new
                    {
                        pair.LeftIndex,
                        pair.RightValue,
                        isCorrect = false
                    });
                    continue;
                }

                var correctRight = data.Pairs[leftIndex].Right;
                var isUniquePair = usedLeftIndexes.Add(leftIndex) &&
                    usedRightValues.Add(pair.RightValue);
                var pairIsCorrect = isUniquePair && correctRight == pair.RightValue;

                if (!pairIsCorrect)
                    isCorrect = false;

                pairResults.Add(new
                {
                    pair.LeftIndex,
                    pair.RightValue,
                    isCorrect = pairIsCorrect
                });
            }

            return await SaveMatchAttempt(attempt, taskId, isCorrect, userId, pairResults);
        }

        await _context.SaveChangesAsync();

        return BadRequest("Невідомий тип задачі");

    }

    [HttpGet]
    public async Task<IActionResult> Run(int attemptId)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
            return Challenge();

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
    string userId,
    object? extraData = null)
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

        var response = new Dictionary<string, object?>
        {
            ["isCorrect"] = isCorrect,
            ["attemptsLeft"] = GetAttemptsLeft(attempt, taskId)
        };

        if (extraData != null)
        {
            foreach (var property in extraData.GetType().GetProperties())
            {
                response[property.Name] = property.GetValue(extraData);
            }
        }

        return Json(response);
    }

    private async Task<IActionResult> SaveMatchAttempt(
        ExerciseAttempt attempt,
        int taskId,
        bool isCorrect,
        string userId,
        IReadOnlyCollection<object> pairResults)
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

        return Json(new
        {
            isCorrect,
            attemptsLeft = GetAttemptsLeft(attempt, taskId),
            pairResults
        });
    }

    private static int GetAttemptsLeft(ExerciseAttempt attempt, int taskId)
    {
        var attemptsCount = attempt.TaskAttempts
            .Count(x => x.ExerciseTaskId == taskId);

        return Math.Max(0, 2 - attemptsCount);
    }

    private static bool CanFinishAttempt(ExerciseAttempt attempt, IEnumerable<int> allTaskIds)
    {
        return allTaskIds.All(taskId =>
        {
            var taskAttempts = attempt.TaskAttempts
                .Where(x => x.ExerciseTaskId == taskId)
                .ToList();

            return taskAttempts.Any(x => x.IsCorrect) || taskAttempts.Count >= 2;
        });
    }

    private static bool TryParseOrder(string? order, out List<int> values)
    {
        values = new List<int>();

        if (string.IsNullOrWhiteSpace(order))
            return false;

        try
        {
            values = order
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(int.Parse)
                .ToList();

            return values.Count == 4 && values.Distinct().Count() == values.Count;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool HasSameItems(IReadOnlyCollection<int> selected, IReadOnlyCollection<int> correct)
    {
        return selected
            .OrderBy(x => x)
            .SequenceEqual(correct.OrderBy(x => x));
    }

}
