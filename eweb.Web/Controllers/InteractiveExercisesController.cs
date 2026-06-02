using eweb.Domain.Entities.Exercises;
using eweb.Infrastructure.Data;
using eweb.Web.Models.Exercises;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace eweb.Web.Controllers;

[Authorize(Roles = "Admin")]
public class InteractiveExercisesController : Controller
{
    private readonly ApplicationDbContext _context;

    public InteractiveExercisesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // LIST

    public async Task<IActionResult> Index()
    {
        var exercises = await _context.InteractiveExercises
            .ToListAsync();

        return View(exercises);
    }

    // CREATE (GET)

    [HttpGet]
    public async Task<IActionResult> CreateFull()
    {
        await LoadAvailableLessons();
        return View(new CreateInteractiveExerciseViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFull(CreateInteractiveExerciseViewModel model)
    {
        await LoadAvailableLessons();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existingCount = await _context.InteractiveExercises
            .CountAsync(e => e.LessonId == model.LessonId);

        try
        {
            InteractiveExercise.EnsureLessonExerciseLimit(existingCount);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }

        try
        {
            ValidateTasks(model.Tasks);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }

        int exerciseOrder = existingCount + 1;

        var exercise = new InteractiveExercise(
            model.LessonId,
            model.Title,
            model.Description,
            exerciseOrder
        );

        for (int i = 0; i < model.Tasks.Count; i++)
        {
            var taskVm = model.Tasks[i];

            string DataJson = BuildTaskJson(taskVm);

            var task = new ExerciseTask(
                taskVm.Type,
                taskVm.QuestionText,
                DataJson,
                taskVm.StarsReward,
                i + 1
            );

            exercise.AddTask(task);
        }

        _context.InteractiveExercises.Add(exercise);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // EDIT (GET)


    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var exercise = await _context.InteractiveExercises
            .Include(e => e.Tasks)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (exercise == null)
            return NotFound();

        try
        {
            exercise.EnsureCanBeEdited();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        var model = MapToEditViewModel(exercise);

        await LoadAvailableLessons(exercise.LessonId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditInteractiveExerciseViewModel model)
    {
        await LoadAvailableLessons();

        if (!ModelState.IsValid)
            return View(model);

        var exercise = await _context.InteractiveExercises
            .Include(e => e.Tasks)
            .FirstOrDefaultAsync(e => e.Id == model.Id);

        if (exercise == null)
            return NotFound();

        try
        {
            exercise.EnsureCanBeEdited();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }

        try
        {
            ValidateTasks(model.Tasks);

            exercise.Update(model.Title, model.Description, exercise.Order);

            _context.ExerciseTasks.RemoveRange(exercise.Tasks);
            exercise.ClearTasks();

            for (int i = 0; i < model.Tasks.Count; i++)
            {
                var taskVm = model.Tasks[i];
                var json = BuildTaskJson(taskVm);

                var task = new ExerciseTask(
                    taskVm.Type,
                    taskVm.QuestionText,
                    json,
                    taskVm.StarsReward,
                    i + 1
                );

                exercise.AddTask(task);
            }

            await _context.SaveChangesAsync();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    private EditInteractiveExerciseViewModel MapToEditViewModel(
    InteractiveExercise exercise)
    {
        var model = new EditInteractiveExerciseViewModel
        {
            Id = exercise.Id,
            LessonId = exercise.LessonId,
            Title = exercise.Title,
            Description = exercise.Description
        };

        foreach (var task in exercise.Tasks.OrderBy(t => t.Order))
        {
            var vm = new ExerciseTaskEditViewModel
            {
                Type = task.Type,
                QuestionText = task.QuestionText,
                StarsReward = task.StarsReward
            };

            ParseJsonToViewModel(task.DataJson, vm);

            model.Tasks.Add(vm);
        }

        return model;
    }

    // DELETE

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var exercise = await _context.InteractiveExercises
            .FirstOrDefaultAsync(e => e.Id == id);

        if (exercise == null)
            return NotFound();

        int lessonId = exercise.LessonId;

        _context.ExerciseTasks.RemoveRange(
            _context.ExerciseTasks.Where(t => t.ExerciseId == id)
        );

        _context.InteractiveExercises.Remove(exercise);
        await _context.SaveChangesAsync();

        // reorder
        var exercises = await _context.InteractiveExercises
            .Where(e => e.LessonId == lessonId)
            .OrderBy(e => e.Order)
            .ToListAsync();

        for (int i = 0; i < exercises.Count; i++)
        {
            exercises[i].Update(
                exercises[i].Title,
                exercises[i].Description,
                i + 1
            );
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // PUBLISH

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id)
    {
        var exercise = await _context.InteractiveExercises
            .Include(e => e.Tasks)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (exercise == null)
            return NotFound();

        try
        {
            exercise.EnsureCanBePublished();
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }

        exercise.Publish();
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // UNPUBLISH

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unpublish(int id)
    {
        var exercise = await _context.InteractiveExercises
            .FirstOrDefaultAsync(e => e.Id == id);

        if (exercise == null)
            return NotFound();

        exercise.Unpublish();
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // HELPERS
    private async Task LoadAvailableLessons(int? currentLessonId = null)
    {
        var lessons = await _context.Lessons
            .Where(l =>
                _context.InteractiveExercises
                    .Count(e => e.LessonId == l.Id) < 2
                || l.Id == currentLessonId)
            .ToListAsync();

        ViewBag.Lessons = lessons;
    }

    private void ParseJsonToViewModel(string json, ExerciseTaskEditViewModel vm)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        switch (vm.Type)
        {
            case ExerciseType.MultipleChoice:
                var options = root.GetProperty("options").EnumerateArray().ToList();

                if (options.Count >= 4)
                {
                    vm.Option1 = options[0].GetProperty("text").GetString();
                    vm.IsOption1Correct = options[0].GetProperty("isCorrect").GetBoolean();

                    vm.Option2 = options[1].GetProperty("text").GetString();
                    vm.IsOption2Correct = options[1].GetProperty("isCorrect").GetBoolean();

                    vm.Option3 = options[2].GetProperty("text").GetString();
                    vm.IsOption3Correct = options[2].GetProperty("isCorrect").GetBoolean();

                    vm.Option4 = options[3].GetProperty("text").GetString();
                    vm.IsOption4Correct = options[3].GetProperty("isCorrect").GetBoolean();
                }
                break;

            case ExerciseType.Reorder:
                var items = root.GetProperty("items").EnumerateArray().ToList();

                if (items.Count >= 4)
                {
                    vm.ReorderItem1 = items[0].GetString();
                    vm.ReorderItem2 = items[1].GetString();
                    vm.ReorderItem3 = items[2].GetString();
                    vm.ReorderItem4 = items[3].GetString();
                }

                if (root.TryGetProperty("correctOrder", out var correctOrder))
                {
                    if (correctOrder.ValueKind == JsonValueKind.Array)
                    {
                        vm.CorrectOrder = string.Join(",",
                            correctOrder.EnumerateArray().Select(x => x.GetInt32()));
                    }
                    else if (correctOrder.ValueKind == JsonValueKind.String)
                    {
                        vm.CorrectOrder = correctOrder.GetString();
                    }
                }
                break;

            case ExerciseType.MatchPairs:
                var pairs = root.GetProperty("pairs").EnumerateArray().ToList();

                if (pairs.Count >= 2)
                {
                    vm.Left1 = pairs[0].GetProperty("left").GetString();
                    vm.Right1 = pairs[0].GetProperty("right").GetString();

                    vm.Left2 = pairs[1].GetProperty("left").GetString();
                    vm.Right2 = pairs[1].GetProperty("right").GetString();

                    if (pairs.Count > 2)
                    {
                        vm.Left3 = pairs[2].GetProperty("left").GetString();
                        vm.Right3 = pairs[2].GetProperty("right").GetString();
                    }

                    if (pairs.Count > 3)
                    {
                        vm.Left4 = pairs[3].GetProperty("left").GetString();
                        vm.Right4 = pairs[3].GetProperty("right").GetString();
                    }
                }
                break;

            case ExerciseType.FillGaps:

                var gapOptions = root.GetProperty("options").EnumerateArray().ToList();

                if (gapOptions.Count >= 4)
                {
                    vm.GapOption1 = gapOptions[0].GetString();
                    vm.GapOption2 = gapOptions[1].GetString();
                    vm.GapOption3 = gapOptions[2].GetString();
                    vm.GapOption4 = gapOptions[3].GetString();
                }

                if (root.TryGetProperty("correctOptionIndex", out var correctIndex))
                {
                    vm.CorrectOptionIndex = correctIndex.GetInt32();
                }

                break;
        }
    }

    private string BuildTaskJson(BaseExerciseTaskViewModel task)
    {
        ValidateTask(task);

        object data = task.Type switch
        {
            ExerciseType.MultipleChoice => new
            {
                options = new[]
                {
                    new { text = task.Option1, isCorrect = task.IsOption1Correct },
                    new { text = task.Option2, isCorrect = task.IsOption2Correct },
                    new { text = task.Option3, isCorrect = task.IsOption3Correct },
                    new { text = task.Option4, isCorrect = task.IsOption4Correct }
                }
            },

            ExerciseType.Reorder => new
            {
                items = new[]
                {
                    task.ReorderItem1,
                    task.ReorderItem2,
                    task.ReorderItem3,
                    task.ReorderItem4
                },
                correctOrder = task.CorrectOrder
            },

            ExerciseType.MatchPairs => new
            {
                pairs = new[]
                {
                    new { left = task.Left1, right = task.Right1 },
                    new { left = task.Left2, right = task.Right2 },
                    new { left = task.Left3, right = task.Right3 },
                    new { left = task.Left4, right = task.Right4 }
                }
            },

            ExerciseType.FillGaps => new
            {
                options = new[]
                {
                    task.GapOption1,
                    task.GapOption2,
                    task.GapOption3,
                    task.GapOption4
                },
                correctOptionIndex = task.CorrectOptionIndex
            },

            _ => throw new InvalidOperationException("Невідомий тип завдання.")
        };

        return JsonSerializer.Serialize(data);
    }

    private static void ValidateTasks(IReadOnlyCollection<BaseExerciseTaskViewModel> tasks)
    {
        if (tasks.Count < 3 || tasks.Count > 5)
            throw new InvalidOperationException("Вправа повинна містити від 3 до 5 завдань.");

        foreach (var task in tasks)
        {
            ValidateTask(task);
        }
    }

    private static void ValidateTask(BaseExerciseTaskViewModel task)
    {
        if (string.IsNullOrWhiteSpace(task.QuestionText))
            throw new InvalidOperationException("Текст питання не може бути порожнім.");

        if (task.StarsReward < 1 || task.StarsReward > 2)
            throw new InvalidOperationException("Кількість зірок має бути від 1 до 2.");

        switch (task.Type)
        {
            case ExerciseType.MultipleChoice:
                ValidateMultipleChoice(task);
                break;

            case ExerciseType.Reorder:
                ValidateReorder(task);
                break;

            case ExerciseType.MatchPairs:
                ValidateMatchPairs(task);
                break;

            case ExerciseType.FillGaps:
                ValidateFillGaps(task);
                break;

            default:
                throw new InvalidOperationException("Невідомий тип завдання.");
        }
    }

    private static void ValidateMultipleChoice(BaseExerciseTaskViewModel task)
    {
        var options = new[] { task.Option1, task.Option2, task.Option3, task.Option4 };

        if (options.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("У завданні з вибором відповіді всі 4 варіанти мають бути заповнені.");

        var correctCount = new[]
        {
            task.IsOption1Correct,
            task.IsOption2Correct,
            task.IsOption3Correct,
            task.IsOption4Correct
        }.Count(x => x);

        if (correctCount == 0)
            throw new InvalidOperationException("У завданні з вибором відповіді має бути хоча б одна правильна відповідь.");

        if (correctCount == options.Length)
            throw new InvalidOperationException("У завданні з вибором відповіді має бути хоча б одна неправильна відповідь.");
    }

    private static void ValidateReorder(BaseExerciseTaskViewModel task)
    {
        var items = new[] { task.ReorderItem1, task.ReorderItem2, task.ReorderItem3, task.ReorderItem4 };

        if (items.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("У завданні на порядок всі 4 елементи мають бути заповнені.");

        var order = ParseCorrectOrder(task.CorrectOrder);

        if (!order.SequenceEqual(new[] { 1, 2, 3, 4 }))
        {
            throw new InvalidOperationException("Правильний порядок має містити числа 1,2,3,4 без повторів.");
        }
    }

    private static void ValidateMatchPairs(BaseExerciseTaskViewModel task)
    {
        var pairs = new[]
        {
            (Left: task.Left1, Right: task.Right1),
            (Left: task.Left2, Right: task.Right2),
            (Left: task.Left3, Right: task.Right3),
            (Left: task.Left4, Right: task.Right4)
        };

        if (pairs.Any(p => string.IsNullOrWhiteSpace(p.Left) || string.IsNullOrWhiteSpace(p.Right)))
            throw new InvalidOperationException("У завданні на пари всі ліві та праві значення мають бути заповнені.");

        if (pairs.Select(p => p.Right!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != pairs.Length)
            throw new InvalidOperationException("Праві значення у парах не мають повторюватися.");
    }

    private static void ValidateFillGaps(BaseExerciseTaskViewModel task)
    {
        var options = new[] { task.GapOption1, task.GapOption2, task.GapOption3, task.GapOption4 };

        if (options.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("У завданні з пропуском всі 4 варіанти мають бути заповнені.");

        if (task.CorrectOptionIndex < 0 || task.CorrectOptionIndex > 3)
            throw new InvalidOperationException("Правильний варіант для пропуску має бути від 1 до 4.");
    }

    private static List<int> ParseCorrectOrder(string? correctOrder)
    {
        if (string.IsNullOrWhiteSpace(correctOrder))
            throw new InvalidOperationException("Правильний порядок має бути заповнений.");

        try
        {
            return correctOrder
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(int.Parse)
                .OrderBy(x => x)
                .ToList();
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("Правильний порядок має містити тільки числа, розділені комами.");
        }
    }
}
