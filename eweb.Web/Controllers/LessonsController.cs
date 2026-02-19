using eweb.Domain.Constants;
using eweb.Domain.Entities;
using eweb.Infrastructure.Data;
using eweb.Web.Models.Lessons;
using eweb.Web.Models.Questions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eweb.Web.Controllers;

[Authorize]
public class LessonsController : Controller
{
    private readonly ApplicationDbContext _context;

    public LessonsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ==============================
    // INDEX
    // ==============================

    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        if (User.Identity != null &&
            User.Identity.IsAuthenticated &&
            User.IsInRole(RoleNames.Admin))
        {
            return View(await _context.Lessons.ToListAsync());
        }

        return View(await _context.Lessons
            .Where(l => l.IsPublished)
            .ToListAsync());
    }

    // ==============================
    // DETAILS (GET)
    // ==============================

    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Questions)
                .ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lesson == null)
            return NotFound();

        var model = new LessonDetailsViewModel
        {
            LessonId = lesson.Id,
            Number = lesson.Number,
            Title = lesson.Title,
            Description = lesson.Description,
            Content = lesson.Content,
            IsPublished = lesson.IsPublished,
            CreatedAt = lesson.CreatedAt,
            Questions = lesson.Questions.Select(q =>
                new LessonDetailsViewModel.QuestionVm
                {
                    QuestionId = q.Id,
                    QuestionText = q.QuestionText,
                    Answers = q.AnswerOptions
                        .OrderBy(a => Guid.NewGuid())
                        .Select(a => new LessonDetailsViewModel.AnswerVm
                        {
                            AnswerId = a.Id,
                            Text = a.Text
                        }).ToList()
                }).ToList()
        };

        return View(model);
    }

    // ==============================
    // DETAILS (POST) — перевірка тесту
    // ==============================

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Details(LessonDetailsViewModel model)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Questions)
                .ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(l => l.Id == model.LessonId);

        if (lesson == null)
            return NotFound();

        int totalCorrectAnswers = lesson.Questions
            .SelectMany(q => q.AnswerOptions)
            .Count(a => a.IsCorrect);

        int userCorrectAnswers = 0;

        foreach (var question in lesson.Questions)
        {
            var userQuestion = model.Questions
                .FirstOrDefault(q => q.QuestionId == question.Id);

            if (userQuestion == null)
                continue;

            foreach (var answer in question.AnswerOptions)
            {
                var userAnswer = userQuestion.Answers
                    .FirstOrDefault(a => a.AnswerId == answer.Id);

                if (userAnswer != null &&
                    userAnswer.IsSelected &&
                    answer.IsCorrect)
                {
                    userCorrectAnswers++;
                }
            }
        }

        double percent = totalCorrectAnswers == 0
            ? 0
            : Math.Round(
                (double)userCorrectAnswers /
                totalCorrectAnswers * 100, 2);

        // перезаповнюємо модель
        var resultModel = new LessonDetailsViewModel
        {
            LessonId = lesson.Id,
            Number = lesson.Number,
            Title = lesson.Title,
            Description = lesson.Description,
            Content = lesson.Content,
            IsPublished = lesson.IsPublished,
            CreatedAt = lesson.CreatedAt,
            ResultPercent = percent,
            Questions = lesson.Questions.Select(q =>
                new LessonDetailsViewModel.QuestionVm
                {
                    QuestionId = q.Id,
                    QuestionText = q.QuestionText,
                    Answers = q.AnswerOptions
                        .OrderBy(a => Guid.NewGuid())
                        .Select(a => new LessonDetailsViewModel.AnswerVm
                        {
                            AnswerId = a.Id,
                            Text = a.Text
                        }).ToList()
                }).ToList()
        };

        return View(resultModel);
    }

    // ==============================
    // CREATE LESSON
    // ==============================

    [Authorize(Roles = RoleNames.Admin)]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateLessonViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var lesson = new Lesson(
                model.Number,
                model.Title,
                model.Description,
                model.Content
            );

            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }
    }

    // ==============================
    // EDIT
    // ==============================

    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var lesson = await _context.Lessons.FindAsync(id);
        if (lesson == null)
            return NotFound();

        var model = new EditLessonViewModel
        {
            Id = lesson.Id,
            Number = lesson.Number,
            Title = lesson.Title,
            Description = lesson.Description,
            Content = lesson.Content,
            IsPublished = lesson.IsPublished
        };

        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditLessonViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var lesson = await _context.Lessons.FindAsync(model.Id);
        if (lesson == null)
            return NotFound();

        lesson.Update(
            model.Number,
            model.Title,
            model.Description,
            model.Content
        );

        if (model.IsPublished)
            lesson.Publish();
        else
            lesson.Unpublish();

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // ==============================
    // DELETE
    // ==============================

    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var lesson = await _context.Lessons
            .FirstOrDefaultAsync(m => m.Id == id);

        if (lesson == null)
            return NotFound();

        return View(lesson);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var lesson = await _context.Lessons.FindAsync(id);

        if (lesson != null)
            _context.Lessons.Remove(lesson);

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}