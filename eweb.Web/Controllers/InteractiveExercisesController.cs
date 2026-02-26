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

            string jsonData = BuildTaskJson(taskVm);

            var task = new ExerciseTask(
                0,
                taskVm.Type,
                taskVm.QuestionText,
                jsonData,
                taskVm.StarsReward,
                i + 1
            );

            exercise.AddTask(task);
        }

        _context.InteractiveExercises.Add(exercise);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
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
    private async Task LoadAvailableLessons()
    {
        var lessons = await _context.Lessons
            .Where(l => _context.InteractiveExercises
                .Count(e => e.LessonId == l.Id) < 2)
            .ToListAsync();

        ViewBag.Lessons = lessons;
    }

    private string BuildTaskJson(ExerciseTaskCreateViewModel task)
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

            _ => throw new Exception("Unsupported task type")
        };

        return JsonSerializer.Serialize(data);
    }
}