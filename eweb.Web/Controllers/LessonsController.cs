using eweb.Domain.Constants;
using eweb.Domain.Entities;
using eweb.Domain.Entities.Attempts;
using eweb.Domain.Entities.Progress;
using eweb.Infrastructure.Data;
using eweb.Infrastructure.Identity;
using eweb.Web.Models.Lessons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eweb.Web.Controllers;

[Authorize]
public class LessonsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public LessonsController(ApplicationDbContext context,UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // INDEX

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

    // DETAILS (GET)

    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Questions)
            .ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lesson == null)
            return NotFound();

        var userId = _userManager.GetUserId(User);
        bool isAdmin = User.IsInRole(RoleNames.Admin);

        int attemptsCount = 0;
        int minAttempts = 3;
        int maxAttempts = 10;

        if (!isAdmin && userId != null)
        {
            var exists = await _context.UserLessonProgresses
                .AnyAsync(x => x.UserId == userId && x.LessonId == id);

            if (!exists)
            {
                var progress = new UserLessonProgress(userId, id);
                _context.UserLessonProgresses.Add(progress);
                await _context.SaveChangesAsync();
            }

            attemptsCount = await _context.LessonTestAttempts
                .CountAsync(x => x.UserId == userId && x.LessonId == id);
        }

        var model = new LessonDetailsViewModel
        {
            LessonId = lesson.Id,
            Number = lesson.Number,
            Title = lesson.Title,
            Description = lesson.Description,
            Content = lesson.Content,
            IsPublished = lesson.IsPublished,
            CreatedAt = lesson.CreatedAt,

            AttemptsCount = attemptsCount,
            ShowCorrectAnswers = isAdmin,
            AttemptsExceeded = !isAdmin && attemptsCount >= maxAttempts,

            Questions = lesson.Questions.Select(q => new LessonDetailsViewModel.QuestionVm
            {
                QuestionId = q.Id,
                QuestionText = q.QuestionText,
                Answers = q.AnswerOptions
                    .OrderBy(_ => Guid.NewGuid())
                    .Select(a => new LessonDetailsViewModel.AnswerVm
                    {
                        AnswerId = a.Id,
                        Text = a.Text,
                        IsSelected = isAdmin && a.IsCorrect
                    }).ToList()

            }).ToList()
        };

        return View(model);
    }

    // DETAILS

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Details(LessonDetailsViewModel model)
    {
        var userId = _userManager.GetUserId(User);
        bool isAdmin = User.IsInRole(RoleNames.Admin);

        var lesson = await _context.Lessons
            .Include(l => l.Questions)
            .ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(l => l.Id == model.LessonId);

        if (lesson == null)
            return NotFound();

        int attemptsCount = 0;
        int minAttempts = 3;
        int maxAttempts = 10;

        if (!isAdmin)
        {
            attemptsCount = await _context.LessonTestAttempts
                .CountAsync(x => x.UserId == userId && x.LessonId == model.LessonId);

            if (attemptsCount >= maxAttempts)
                return RedirectToAction(nameof(Details), new { id = model.LessonId });
        }

        LessonTestAttempt? attempt = null;

        if (!isAdmin)
        {
            attempt = new LessonTestAttempt(userId!, lesson.Id);
            _context.LessonTestAttempts.Add(attempt);
        }

        int totalQuestions = lesson.Questions.Count;
        int completedQuestions = 0;

        foreach (var question in lesson.Questions)
        {
            var userQuestion = model.Questions
                .FirstOrDefault(q => q.QuestionId == question.Id);

            if (userQuestion == null)
                continue;

            var correctAnswerIds = question.AnswerOptions
                .Where(a => a.IsCorrect)
                .Select(a => a.Id)
                .ToList();

            var selectedAnswerIds = userQuestion.Answers
                .Where(a => a.IsSelected)
                .Select(a => a.AnswerId)
                .ToList();

            bool isCorrect =
                selectedAnswerIds.Count == correctAnswerIds.Count &&
                selectedAnswerIds.All(id => correctAnswerIds.Contains(id));

            if (isCorrect)
            {
                completedQuestions++;

                if (!isAdmin)
                {
                    var exists = await _context.UserQuestionProgresses
                        .AnyAsync(x => x.UserId == userId && x.QuestionId == question.Id);

                    if (!exists)
                    {
                        var progress = new UserQuestionProgress(userId!, question.Id);
                        _context.UserQuestionProgresses.Add(progress);
                    }
                }
            }
        }

        double percent = totalQuestions == 0
            ? 0
            : Math.Round((double)completedQuestions / totalQuestions * 100, 2);

        if (!isAdmin && attempt != null)
            attempt.Finish(percent);

        await _context.SaveChangesAsync();

        if (!isAdmin)
            attemptsCount++;

        bool showModal = !isAdmin && attemptsCount >= minAttempts && percent < 100;

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
            AttemptsCount = attemptsCount,
            ShowCorrectAnswers = isAdmin,
            ShowCorrectAnswersModal = showModal,
            AttemptsExceeded = !isAdmin && attemptsCount >= maxAttempts,
            Questions = lesson.Questions.Select(q => new LessonDetailsViewModel.QuestionVm
            {
                QuestionId = q.Id,
                QuestionText = q.QuestionText,
                Answers = q.AnswerOptions
                    .OrderBy(_ => Guid.NewGuid())
                    .Select(a => new LessonDetailsViewModel.AnswerVm
                    {
                        AnswerId = a.Id,
                        Text = a.Text,
                        IsSelected = (isAdmin || showModal) && a.IsCorrect
                    }).ToList()
            }).ToList()
        };

        return View(resultModel);
    }

    // CREATE

    [Authorize(Roles = RoleNames.Admin)]
    public IActionResult Create()
    {
        var model = new CreateLessonViewModel();

        model.Questions.Add(new QuestionInputModel
        {
            Answers = new List<AnswerInputModel>
        {
            new(),
            new()
        }
        });

        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateLessonViewModel model)
    {
        ModelState.Clear();

        try
        {
            var lesson = new Lesson(
                model.Number,
                model.Title,
                model.Description,
                model.Content
            );

            if (model.Questions.Count > 10)
                throw new InvalidOperationException("Урок не може мати більше 10 питань.");

            foreach (var q in model.Questions)
            {
                if (string.IsNullOrWhiteSpace(q.QuestionText))
                    continue;

                var question = new TheoryQuestion(q.QuestionText, lesson.Id);

                foreach (var a in q.Answers)
                {
                    if (string.IsNullOrWhiteSpace(a.Text))
                        continue;

                    question.AddAnswerOption(a.Text, a.IsCorrect);
                }

                lesson.AddQuestion(question);
            }

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

    // EDIT

    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Questions)
            .ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (lesson == null)
            return NotFound();

        var model = new EditLessonViewModel
        {
            Id = lesson.Id,
            Number = lesson.Number,
            Title = lesson.Title,
            Description = lesson.Description,
            Content = lesson.Content,
            IsPublished = lesson.IsPublished,

            Questions = lesson.Questions.Select(q => new QuestionEditModel
            {
                Id = q.Id,
                QuestionText = q.QuestionText,

                Answers = q.AnswerOptions.Select(a => new AnswerEditModel
                {
                    Id = a.Id,
                    Text = a.Text,
                    IsCorrect = a.IsCorrect
                }).ToList()

            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditLessonViewModel model)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Questions)
            .ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(l => l.Id == model.Id);

        if (lesson == null)
            return NotFound();

        if (model.IsPublished)
        {
            var validQuestions = model.Questions
                .Where(q => !string.IsNullOrWhiteSpace(q.QuestionText))
                .ToList();

            if (validQuestions.Count == 0)
                ModelState.AddModelError("", "Щоб опублікувати урок, він повинен мати хоча б одне питання.");

            foreach (var q in validQuestions)
            {
                var validAnswers = q.Answers
                    .Where(a => !string.IsNullOrWhiteSpace(a.Text))
                    .ToList();

                if (validAnswers.Count < 2)
                    ModelState.AddModelError("",
                        $"Питання «{q.QuestionText}» повинно мати щонайменше 2 варіанти відповіді.");

                if (!validAnswers.Any(a => a.IsCorrect))
                    ModelState.AddModelError("",
                        $"Питання «{q.QuestionText}» повинно мати хоча б одну правильну відповідь.");
            }

            if (!ModelState.IsValid)
                return View(model);
        }

        try
        {
            lesson.Unpublish();

            lesson.Update(
                model.Number,
                model.Title,
                model.Description,
                model.Content
            );

            _context.TheoryQuestions.RemoveRange(lesson.Questions);
            await _context.SaveChangesAsync();

            lesson = await _context.Lessons
                .Include(l => l.Questions)
                .ThenInclude(q => q.AnswerOptions)
                .FirstOrDefaultAsync(l => l.Id == model.Id);

            foreach (var q in model.Questions)
            {
                if (string.IsNullOrWhiteSpace(q.QuestionText)) continue;

                var question = new TheoryQuestion(q.QuestionText, lesson!.Id);

                foreach (var a in q.Answers)
                {
                    if (string.IsNullOrWhiteSpace(a.Text)) continue;
                    question.AddAnswerOption(a.Text, a.IsCorrect);
                }

                lesson.AddQuestion(question);
            }

            await _context.SaveChangesAsync();

            if (model.IsPublished)
                lesson!.Publish();
            else
                lesson!.Unpublish();

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }
    }

    // DELETE

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

    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> ShowCorrectAnswers(int lessonId)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Questions)
            .ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(l => l.Id == lessonId);

        if (lesson == null)
            return NotFound();

        var model = new LessonDetailsViewModel
        {
            LessonId = lesson.Id,
            Number = lesson.Number,
            Title = lesson.Title,
            Description = lesson.Description,
            Content = lesson.Content,
            Questions = lesson.Questions.Select(q => new LessonDetailsViewModel.QuestionVm
            {
                QuestionId = q.Id,
                QuestionText = q.QuestionText,
                Answers = q.AnswerOptions.Select(a => new LessonDetailsViewModel.AnswerVm
                {
                    AnswerId = a.Id,
                    Text = a.Text,
                    IsSelected = a.IsCorrect
                }).ToList()

            }).ToList()
        };

        return View("Details", model);
    }
}