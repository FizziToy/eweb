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

    // ============================
    // LIST
    // ============================
    public async Task<IActionResult> Index()
    {
        var exercises = await _context.InteractiveExercises
            .ToListAsync();

        return View(exercises);
    }

    // ============================
    // CREATE (GET)
    // ============================
    [HttpGet]
    public async Task<IActionResult> CreateFull()
    {
        await LoadAvailableLessons();
        return View(new CreateInteractiveExerciseViewModel());
    }

    // ============================
    // CREATE (POST)
    // ============================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFull(CreateInteractiveExerciseViewModel model)
    {
        await LoadAvailableLessons();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Ліміт 2 вправи на урок
        var existingCount = await _context.InteractiveExercises
            .CountAsync(e => e.LessonId == model.LessonId);

        if (existingCount >= 2)
        {
            ModelState.AddModelError("", "This lesson already has 2 exercises.");
            return View(model);
        }

        int exerciseOrder = existingCount + 1;

        var exercise = new InteractiveExercise(
            model.LessonId,
            model.Title,
            model.Description,
            exerciseOrder
        );

        // Додаємо задачі
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

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var exercise = await _context.InteractiveExercises
            .Include(e => e.Tasks)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (exercise == null)
            return NotFound();

        if (exercise.IsPublished)
            return BadRequest("Unpublish before editing.");

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

        if (exercise.IsPublished)
        {
            ModelState.AddModelError("",
                "Unpublish exercise before editing.");
            return View(model);
        }

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

    // ============================
    // DELETE
    // ============================
    [HttpPost]
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

    // ============================
    // PUBLISH
    // ============================
    [HttpPost]
    public async Task<IActionResult> Publish(int id)
    {
        var exercise = await _context.InteractiveExercises
            .Include(e => e.Tasks)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (exercise == null)
            return NotFound();

        if (exercise.Tasks.Count < 3 || exercise.Tasks.Count > 5)
        {
            TempData["Error"] = "Exercise must contain 3–5 tasks before publishing.";
            return RedirectToAction(nameof(Index));
        }

        exercise.Publish();
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // ============================
    // UNPUBLISH
    // ============================
    [HttpPost]
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

    // ============================
    // HELPERS
    // ============================
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
                    vm.CorrectOrder = correctOrder.GetString();
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

            _ => throw new Exception("Unsupported task type")
        };

        return JsonSerializer.Serialize(data);
    }
}