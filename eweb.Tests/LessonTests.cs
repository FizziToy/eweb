using System.Security.Claims;
using eweb.Domain.Constants;
using eweb.Domain.Entities;
using eweb.Domain.Entities.Progress;
using eweb.Infrastructure.Data;
using eweb.Infrastructure.Identity;
using eweb.Web.Controllers;
using eweb.Web.Models.Lessons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace eweb.Tests;

public class LessonTests                        
{
    private const string UserId = "user-1";
    private const string AdminId = "admin-1";

    private static QuestionInputModel CreateQuestionInput(string questionText)
    {
        return new QuestionInputModel
        {
            QuestionText = questionText,
            Answers = new List<AnswerInputModel>
        {
            new() { Text = "Правильно", IsCorrect = true },
            new() { Text = "Неправильно", IsCorrect = false }
        }
        };
    }

    [Fact]
    public async Task Create_Get_WhenCategoriesExist_ReturnsDefaultModel()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        AddLesson(db, category.Id, 1, false, "Урок 1");
        AddLesson(db, category.Id, 2, false, "Урок 2");

        var controller = CreateController(db, isAdmin: true);

        var result = await controller.Create();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<CreateLessonViewModel>(view.Model);

        Assert.Equal(3, model.Number);
        Assert.Equal(3, model.MaxNumber);
        Assert.Equal(category.Id, model.CategoryId);
        Assert.NotNull(model.Categories);
        Assert.Single(model.Questions);
        Assert.Equal(2, model.Questions[0].Answers.Count);
    }

    [Fact]
    public async Task Create_WithIsPublishedTrue_CreatesPublishedLesson()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        var controller = CreateController(db, isAdmin: true);

        var model = new CreateLessonViewModel
        {
            Number = 1,
            Title = "Новий урок",
            Description = "Опис уроку",
            Content = "Контент уроку",
            CategoryId = category.Id,
            CreatedAt = new DateOnly(2026, 1, 1),
            IsPublished = true,
            Questions = CreateTwoQuestionInputs()
        };

        var result = await controller.Create(model);

        Assert.IsType<RedirectToActionResult>(result);

        var lesson = await db.Lessons
            .Include(x => x.Questions)
            .ThenInclude(x => x.AnswerOptions)
            .FirstOrDefaultAsync(x => x.Title == "Новий урок");

        Assert.NotNull(lesson);
        Assert.True(lesson!.IsPublished);
        Assert.Equal(2, lesson.Questions.Count);
        Assert.All(lesson.Questions, q => Assert.Equal(2, q.AnswerOptions.Count));
    }

    [Fact]
    public async Task Create_WithIsPublishedFalse_CreatesDraftLesson()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        var controller = CreateController(db, isAdmin: true);

        var model = new CreateLessonViewModel
        {
            Number = 1,
            Title = "Чернетка уроку",
            Description = "Опис уроку",
            Content = "Контент уроку",
            CategoryId = category.Id,
            CreatedAt = new DateOnly(2026, 1, 1),
            IsPublished = false,
            Questions = new List<QuestionInputModel>
        {
            CreateQuestionInput()
        }
        };

        var result = await controller.Create(model);

        Assert.IsType<RedirectToActionResult>(result);

        var lesson = await db.Lessons
            .FirstOrDefaultAsync(x => x.Title == "Чернетка уроку");

        Assert.NotNull(lesson);
        Assert.False(lesson!.IsPublished);
    }

    [Fact]
    public async Task Create_DraftWithoutQuestions_CreatesDraftLesson()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        var controller = CreateController(db, isAdmin: true);

        var model = new CreateLessonViewModel
        {
            Number = 1,
            Title = "Чернетка без питань",
            Description = "Опис",
            Content = "Контент",
            CategoryId = category.Id,
            CreatedAt = new DateOnly(2026, 1, 1),
            IsPublished = false,
            Questions = new List<QuestionInputModel>()
        };

        var result = await controller.Create(model);

        Assert.IsType<RedirectToActionResult>(result);

        var lesson = await db.Lessons
            .Include(x => x.Questions)
            .FirstOrDefaultAsync(x => x.Title == "Чернетка без питань");

        Assert.NotNull(lesson);
        Assert.False(lesson!.IsPublished);
        Assert.Empty(lesson.Questions);
    }

    [Fact]
    public async Task Create_PublishedWithoutQuestions_ReturnsViewAndDoesNotCreateLesson()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        var controller = CreateController(db, isAdmin: true);

        var model = new CreateLessonViewModel
        {
            Number = 1,
            Title = "Опублікований без питань",
            Description = "Опис",
            Content = "Контент",
            CategoryId = category.Id,
            CreatedAt = new DateOnly(2026, 1, 1),
            IsPublished = true,
            Questions = new List<QuestionInputModel>()
        };

        var result = await controller.Create(model);

        Assert.IsType<ViewResult>(result);

        bool exists = await db.Lessons
            .AnyAsync(x => x.Title == "Опублікований без питань");

        Assert.False(exists);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Create_PublishedWithEmptyQuestion_ReturnsViewAndDoesNotCreateLesson()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        var controller = CreateController(db, isAdmin: true);

        var model = new CreateLessonViewModel
        {
            Number = 1,
            Title = "Опублікований з пустим питанням",
            Description = "Опис",
            Content = "Контент",
            CategoryId = category.Id,
            CreatedAt = new DateOnly(2026, 1, 1),
            IsPublished = true,
            Questions = new List<QuestionInputModel>
        {
            new()
            {
                QuestionText = "",
                Answers = new List<AnswerInputModel>
                {
                    new() { Text = "Правильно", IsCorrect = true },
                    new() { Text = "Неправильно", IsCorrect = false }
                }
            }
        }
        };

        var result = await controller.Create(model);

        Assert.IsType<ViewResult>(result);

        bool exists = await db.Lessons
            .AnyAsync(x => x.Title == "Опублікований з пустим питанням");

        Assert.False(exists);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Create_WithQuestionWithoutCorrectAnswer_ReturnsViewAndDoesNotCreateLesson()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        var controller = CreateController(db, isAdmin: true);

        var model = new CreateLessonViewModel
        {
            Number = 1,
            Title = "Питання без правильної відповіді",
            Description = "Опис",
            Content = "Контент",
            CategoryId = category.Id,
            CreatedAt = new DateOnly(2026, 1, 1),
            IsPublished = false,
            Questions = new List<QuestionInputModel>
        {
            new()
            {
                QuestionText = "Питання?",
                Answers = new List<AnswerInputModel>
                {
                    new() { Text = "Варіант 1", IsCorrect = false },
                    new() { Text = "Варіант 2", IsCorrect = false }
                }
            }
        }
        };

        var result = await controller.Create(model);

        Assert.IsType<ViewResult>(result);

        bool exists = await db.Lessons
            .AnyAsync(x => x.Title == "Питання без правильної відповіді");

        Assert.False(exists);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Create_WithQuestionWithoutIncorrectAnswer_ReturnsViewAndDoesNotCreateLesson()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        var controller = CreateController(db, isAdmin: true);

        var model = new CreateLessonViewModel
        {
            Number = 1,
            Title = "Питання без неправильної відповіді",
            Description = "Опис",
            Content = "Контент",
            CategoryId = category.Id,
            CreatedAt = new DateOnly(2026, 1, 1),
            IsPublished = false,
            Questions = new List<QuestionInputModel>
        {
            new()
            {
                QuestionText = "Питання?",
                Answers = new List<AnswerInputModel>
                {
                    new() { Text = "Правильно 1", IsCorrect = true },
                    new() { Text = "Правильно 2", IsCorrect = true }
                }
            }
        }
        };

        var result = await controller.Create(model);

        Assert.IsType<ViewResult>(result);

        bool exists = await db.Lessons
            .AnyAsync(x => x.Title == "Питання без неправильної відповіді");

        Assert.False(exists);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Create_WithQuestionHavingOnlyOneAnswer_ReturnsViewAndDoesNotCreateLesson()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        var controller = CreateController(db, isAdmin: true);

        var model = new CreateLessonViewModel
        {
            Number = 1,
            Title = "Питання з однією відповіддю",
            Description = "Опис",
            Content = "Контент",
            CategoryId = category.Id,
            CreatedAt = new DateOnly(2026, 1, 1),
            IsPublished = false,
            Questions = new List<QuestionInputModel>
        {
            new()
            {
                QuestionText = "Питання?",
                Answers = new List<AnswerInputModel>
                {
                    new() { Text = "Єдина відповідь", IsCorrect = true }
                }
            }
        }
        };

        var result = await controller.Create(model);

        Assert.IsType<ViewResult>(result);

        bool exists = await db.Lessons
            .AnyAsync(x => x.Title == "Питання з однією відповіддю");

        Assert.False(exists);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Create_WithMoreThanTenQuestions_ReturnsViewAndDoesNotCreateLesson()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        var controller = CreateController(db, isAdmin: true);

        var questions = Enumerable.Range(1, 11)
            .Select(i => CreateQuestionInput($"Питання {i}?"))
            .ToList();

        var model = new CreateLessonViewModel
        {
            Number = 1,
            Title = "Урок з 11 питаннями",
            Description = "Опис",
            Content = "Контент",
            CategoryId = category.Id,
            CreatedAt = new DateOnly(2026, 1, 1),
            IsPublished = false,
            Questions = questions
        };

        var result = await controller.Create(model);

        Assert.IsType<ViewResult>(result);

        bool exists = await db.Lessons
            .AnyAsync(x => x.Title == "Урок з 11 питаннями");

        Assert.False(exists);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Create_WithFourCorrectAnswers_ReturnsViewAndDoesNotCreateLesson()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        var controller = CreateController(db, isAdmin: true);

        var model = new CreateLessonViewModel
        {
            Number = 1,
            Title = "Питання з 4 правильною відповіддю",
            Description = "Опис",
            Content = "Контент",
            CategoryId = category.Id,
            CreatedAt = new DateOnly(2026, 1, 1),
            IsPublished = false,
            Questions = new List<QuestionInputModel>
        {
            new()
            {
                QuestionText = "Питання?",
                Answers = new List<AnswerInputModel>
                {
                    new() { Text = "Правильно 1", IsCorrect = true },
                    new() { Text = "Правильно 2", IsCorrect = true },
                    new() { Text = "Правильно 3", IsCorrect = true },
                    new() { Text = "Правильно 4", IsCorrect = true },
                    new() { Text = "Неправильно", IsCorrect = false }
                }
            }
        }
        };

        var result = await controller.Create(model);

        Assert.IsType<ViewResult>(result);

        bool exists = await db.Lessons
            .AnyAsync(x => x.Title == "Питання з 4 правильною відповіддю");

        Assert.False(exists);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Edit_WithNonExistingCategory_ReturnsViewAndDoesNotChangeLesson()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        var lesson = AddLesson(db, category.Id, 1, false, "Початкова назва");

        var controller = CreateController(db, isAdmin: true);

        var model = new EditLessonViewModel
        {
            Id = lesson.Id,
            Number = 1,
            Title = "Змінена назва",
            Description = "Новий опис",
            Content = "Новий контент",
            CategoryId = 999,
            IsPublished = false,
            Questions = CreateTwoQuestionEdits()
        };

        var result = await controller.Edit(model);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);

        var lessonFromDb = await db.Lessons
            .FirstAsync(x => x.Id == lesson.Id);

        Assert.Equal("Початкова назва", lessonFromDb.Title);
        Assert.Equal(category.Id, lessonFromDb.CategoryId);
        Assert.Equal(1, lessonFromDb.Number);
    }

    [Fact]
    public async Task Create_AfterDeleteMiddleQuestionAndAddNew_SavesReindexedQuestionsCorrectly()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        var controller = CreateController(db, isAdmin: true);

        var model = new CreateLessonViewModel
        {
            Number = 1,
            Title = "Урок після переіндексації",
            Description = "Опис уроку",
            Content = "Контент уроку",
            CategoryId = category.Id,
            CreatedAt = new DateOnly(2026, 1, 1),
            IsPublished = true,

            Questions = new List<QuestionInputModel>
        {
            CreateQuestionInput("Питання 1?"),
            CreateQuestionInput("Питання 3?"),
            CreateQuestionInput("Питання 4?")
        }
        };

        var result = await controller.Create(model);

        Assert.IsType<RedirectToActionResult>(result);

        var lesson = await db.Lessons
            .Include(x => x.Questions)
            .ThenInclude(x => x.AnswerOptions)
            .FirstOrDefaultAsync(x => x.Title == "Урок після переіндексації");

        Assert.NotNull(lesson);
        Assert.True(lesson!.IsPublished);

        Assert.Equal(3, lesson.Questions.Count);

        var questionTexts = lesson.Questions
            .Select(x => x.QuestionText)
            .ToList();

        Assert.Contains("Питання 1?", questionTexts);
        Assert.Contains("Питання 3?", questionTexts);
        Assert.Contains("Питання 4?", questionTexts);

        Assert.DoesNotContain("Питання 2?", questionTexts);

        Assert.All(lesson.Questions, q =>
        {
            Assert.Equal(2, q.AnswerOptions.Count);
            Assert.Contains(q.AnswerOptions, a => a.IsCorrect);
            Assert.Contains(q.AnswerOptions, a => !a.IsCorrect);
        });
    }

    [Fact]
    public async Task Create_WithNonExistingCategory_ReturnsViewAndDoesNotCreateLesson()
    {
        using var db = CreateDbContext();
        AddCategory(db);

        var controller = CreateController(db, isAdmin: true);

        var model = new CreateLessonViewModel
        {
            Number = 1,
            Title = "Новий урок",
            Description = "Опис уроку",
            Content = "Контент уроку",
            CategoryId = 999,
            CreatedAt = new DateOnly(2026, 1, 1),
            IsPublished = true,
            Questions = CreateTwoQuestionInputs()
        };

        var result = await controller.Create(model);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);

        var lessonsCount = await db.Lessons.CountAsync();

        Assert.Equal(0, lessonsCount);
    }

    [Fact]
    public async Task Create_WithEmptyAnswerRows_IgnoresEmptyAnswersAndCreatesLesson()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        var controller = CreateController(db, isAdmin: true);

        var model = new CreateLessonViewModel
        {
            Number = 1,
            Title = "Урок з пустими рядками відповідей",
            Description = "Опис",
            Content = "Контент",
            CategoryId = category.Id,
            CreatedAt = new DateOnly(2026, 1, 1),
            IsPublished = true,
            Questions = new List<QuestionInputModel>
            {
                new()
                {
                    QuestionText = "Питання з пустими рядками?",
                    Answers = new List<AnswerInputModel>
                    {
                        new() { Text = "Правильно", IsCorrect = true },
                        new() { Text = "Неправильно", IsCorrect = false },
                        new() { Text = "", IsCorrect = true },
                        new() { Text = "   ", IsCorrect = false }
                    }
                },
                CreateQuestionInput("Друге питання?")
            }
        };

        var result = await controller.Create(model);

        Assert.IsType<RedirectToActionResult>(result);

        var lesson = await db.Lessons
            .Include(x => x.Questions)
            .ThenInclude(x => x.AnswerOptions)
            .FirstOrDefaultAsync(x => x.Title == "Урок з пустими рядками відповідей");

        Assert.NotNull(lesson);
        Assert.True(lesson!.IsPublished);
        Assert.Equal(2, lesson.Questions.Count);

        var questionWithEmptyRows = Assert.Single(
            lesson.Questions.Where(q => q.QuestionText == "Питання з пустими рядками?"));

        Assert.Equal(2, questionWithEmptyRows.AnswerOptions.Count);
    }

    [Fact]
    public async Task Create_TrimsTitleDescriptionAndContent()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        var controller = CreateController(db, isAdmin: true);

        var model = new CreateLessonViewModel
        {
            Number = 1,
            Title = "  Назва з пробілами  ",
            Description = "  Опис з пробілами  ",
            Content = "  Контент з пробілами  ",
            CategoryId = category.Id,
            CreatedAt = new DateOnly(2026, 1, 1),
            IsPublished = false,
            Questions = new List<QuestionInputModel>
        {
            CreateQuestionInput()
        }
        };

        await controller.Create(model);

        var lesson = await db.Lessons
            .FirstOrDefaultAsync(x => x.Title == "Назва з пробілами");

        Assert.NotNull(lesson);
        Assert.Equal("Назва з пробілами", lesson!.Title);
        Assert.Equal("Опис з пробілами", lesson.Description);
        Assert.Equal("Контент з пробілами", lesson.Content);
    }

    [Fact]
    public async Task Create_WithInvalidNumberZero_ReturnsViewAndDoesNotCreateLesson()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        var controller = CreateController(db, isAdmin: true);

        var model = new CreateLessonViewModel
        {
            Number = 0,
            Title = "Урок з неправильним номером",
            Description = "Опис",
            Content = "Контент",
            CategoryId = category.Id,
            CreatedAt = new DateOnly(2026, 1, 1),
            IsPublished = false,
            Questions = new List<QuestionInputModel>
        {
            CreateQuestionInput()
        }
        };

        var result = await controller.Create(model);

        Assert.IsType<ViewResult>(result);

        bool exists = await db.Lessons
            .AnyAsync(x => x.Title == "Урок з неправильним номером");

        Assert.False(exists);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Create_WithInvalidCategory_ReturnsViewAndDoesNotCreateLesson()
    {
        using var db = CreateDbContext();

        var controller = CreateController(db, isAdmin: true);

        var model = new CreateLessonViewModel
        {
            Number = 1,
            Title = "Урок без категорії",
            Description = "Опис",
            Content = "Контент",
            CategoryId = 0,
            CreatedAt = new DateOnly(2026, 1, 1),
            IsPublished = false,
            Questions = new List<QuestionInputModel>
        {
            CreateQuestionInput()
        }
        };

        var result = await controller.Create(model);

        Assert.IsType<ViewResult>(result);

        bool exists = await db.Lessons
            .AnyAsync(x => x.Title == "Урок без категорії");

        Assert.False(exists);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Create_InsertAtFirst_ShiftsAllExistingLessons()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        AddLesson(db, category.Id, 1, false, "Урок 1");
        AddLesson(db, category.Id, 2, false, "Урок 2");
        AddLesson(db, category.Id, 3, false, "Урок 3");

        var controller = CreateController(db, isAdmin: true);

        var model = new CreateLessonViewModel
        {
            Number = 1,
            Title = "Новий перший урок",
            Description = "Опис",
            Content = "Контент",
            CategoryId = category.Id,
            CreatedAt = new DateOnly(2026, 1, 1),
            IsPublished = false,
            Questions = new List<QuestionInputModel>
        {
            CreateQuestionInput()
        }
        };

        await controller.Create(model);

        var ordered = await db.Lessons
            .OrderBy(x => x.Number)
            .Select(x => $"{x.Number}:{x.Title}")
            .ToListAsync();

        Assert.Equal(
            new[]
            {
            "1:Новий перший урок",
            "2:Урок 1",
            "3:Урок 2",
            "4:Урок 3"
            },
            ordered);
    }

    [Fact]
    public async Task Create_InsertInMiddle_ShiftsOnlyLessonsAfterNewPosition()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        AddLesson(db, category.Id, 1, false, "Урок 1");
        AddLesson(db, category.Id, 2, false, "Урок 2");
        AddLesson(db, category.Id, 3, false, "Урок 3");

        var controller = CreateController(db, isAdmin: true);

        var model = new CreateLessonViewModel
        {
            Number = 2,
            Title = "Новий другий урок",
            Description = "Опис",
            Content = "Контент",
            CategoryId = category.Id,
            CreatedAt = new DateOnly(2026, 1, 1),
            IsPublished = false,
            Questions = new List<QuestionInputModel>
        {
            CreateQuestionInput()
        }
        };

        await controller.Create(model);

        var ordered = await db.Lessons
            .OrderBy(x => x.Number)
            .Select(x => $"{x.Number}:{x.Title}")
            .ToListAsync();

        Assert.Equal(
            new[]
            {
            "1:Урок 1",
            "2:Новий другий урок",
            "3:Урок 2",
            "4:Урок 3"
            },
            ordered);
    }

    [Fact]
    public async Task Create_InsertAtEnd_DoesNotShiftExistingLessons()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        AddLesson(db, category.Id, 1, false, "Урок 1");
        AddLesson(db, category.Id, 2, false, "Урок 2");

        var controller = CreateController(db, isAdmin: true);

        var model = new CreateLessonViewModel
        {
            Number = 3,
            Title = "Новий останній урок",
            Description = "Опис",
            Content = "Контент",
            CategoryId = category.Id,
            CreatedAt = new DateOnly(2026, 1, 1),
            IsPublished = false,
            Questions = new List<QuestionInputModel>
        {
            CreateQuestionInput()
        }
        };

        await controller.Create(model);

        var ordered = await db.Lessons
            .OrderBy(x => x.Number)
            .Select(x => $"{x.Number}:{x.Title}")
            .ToListAsync();

        Assert.Equal(
            new[]
            {
            "1:Урок 1",
            "2:Урок 2",
            "3:Новий останній урок"
            },
            ordered);
    }

    [Fact]
    public async Task Edit_WhenErrorHappens_DoesNotShiftExistingLessonNumbers()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        AddLesson(db, category.Id, 1, false, "Урок 1");
        var lesson2 = AddLesson(db, category.Id, 2, false, "Урок 2");
        AddLesson(db, category.Id, 3, false, "Урок 3");

        var controller = CreateController(db, isAdmin: true);

        var model = new EditLessonViewModel
        {
            Id = lesson2.Id,
            Number = 1,
            Title = "Поганий урок",
            Description = "Опис",
            Content = "Контент",
            CategoryId = category.Id,
            IsPublished = true,
            Questions = new List<QuestionEditModel>()
        };

        var result = await controller.Edit(model);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);

        var ordered = await db.Lessons
            .OrderBy(x => x.Number)
            .Select(x => $"{x.Number}:{x.Title}")
            .ToListAsync();

        Assert.Equal(
            new[]
            {
            "1:Урок 1",
            "2:Урок 2",
            "3:Урок 3"
            },
            ordered);
    }

    [Fact]
    public async Task Edit_WithNumberGreaterThanLessonsCount_ReturnsViewAndDoesNotChangeNumbers()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        var lesson1 = AddLesson(db, category.Id, 1, false, "Урок 1");
        AddLesson(db, category.Id, 2, false, "Урок 2");

        var controller = CreateController(db, isAdmin: true);

        var model = new EditLessonViewModel
        {
            Id = lesson1.Id,
            Number = 10,
            Title = "Змінена назва",
            Description = "Опис",
            Content = "Контент",
            CategoryId = category.Id,
            IsPublished = false,
            Questions = new List<QuestionEditModel>
        {
            CreateQuestionEdit()
        }
        };

        var result = await controller.Edit(model);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);

        var ordered = await db.Lessons
            .OrderBy(x => x.Number)
            .Select(x => $"{x.Number}:{x.Title}")
            .ToListAsync();

        Assert.Equal(
            new[]
            {
            "1:Урок 1",
            "2:Урок 2"
            },
            ordered);
    }

    [Fact]
    public async Task Create_WithNumberGreaterThanLastPlusOne_ReturnsViewAndDoesNotCreateGap()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        AddLesson(db, category.Id, 1, false, "Урок 1");
        AddLesson(db, category.Id, 2, false, "Урок 2");

        var controller = CreateController(db, isAdmin: true);

        var model = new CreateLessonViewModel
        {
            Number = 10,
            Title = "Урок з розривом у номерах",
            Description = "Опис",
            Content = "Контент",
            CategoryId = category.Id,
            CreatedAt = new DateOnly(2026, 1, 1),
            IsPublished = false,
            Questions = new List<QuestionInputModel>
        {
            CreateQuestionInput()
        }
        };

        var result = await controller.Create(model);

        Assert.IsType<ViewResult>(result);

        bool exists = await db.Lessons
            .AnyAsync(x => x.Title == "Урок з розривом у номерах");

        Assert.False(exists);
        Assert.False(controller.ModelState.IsValid);

        var numbers = await db.Lessons
            .OrderBy(x => x.Number)
            .Select(x => x.Number)
            .ToListAsync();

        Assert.Equal(new[] { 1, 2 }, numbers);
    }

    [Fact]
    public async Task Create_WhenErrorHappens_DoesNotShiftExistingLessonNumbers()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        AddLesson(db, category.Id, 1, false, "Урок 1");
        AddLesson(db, category.Id, 2, false, "Урок 2");

        var controller = CreateController(db, isAdmin: true);

        var model = new CreateLessonViewModel
        {
            Number = 1,
            Title = "Поганий урок",
            Description = "Опис",
            Content = "Контент",
            CategoryId = category.Id,
            CreatedAt = new DateOnly(2026, 1, 1),
            IsPublished = true,
            Questions = new List<QuestionInputModel>()
        };

        var result = await controller.Create(model);

        Assert.IsType<ViewResult>(result);

        var ordered = await db.Lessons
            .OrderBy(x => x.Number)
            .Select(x => $"{x.Number}:{x.Title}")
            .ToListAsync();

        Assert.Equal(
            new[]
            {
            "1:Урок 1",
            "2:Урок 2"
            },
            ordered);
    }

    [Fact]
    public async Task Index_ForUser_ShowsOnlyPublishedLessons()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        var lesson1 = AddLesson(db, category.Id, 1, true, "Урок 1");
        var lesson2 = AddLesson(db, category.Id, 2, false, "Чернетка");
        var lesson3 = AddLesson(db, category.Id, 3, true, "Урок 3");

        var controller = CreateController(db, isAdmin: false);

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Lesson>>(view.Model);
        var ids = model.Select(x => x.Id).ToList();

        Assert.Equal(new[] { lesson1.Id, lesson3.Id }, ids);
        Assert.DoesNotContain(lesson2.Id, ids);
    }

    [Fact]
    public async Task Index_ForAdmin_ShowsPublishedAndDraftLessons()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        var lesson1 = AddLesson(db, category.Id, 1, true, "Урок 1");
        var lesson2 = AddLesson(db, category.Id, 2, false, "Чернетка");
        var lesson3 = AddLesson(db, category.Id, 3, true, "Урок 3");

        var controller = CreateController(db, isAdmin: true);

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Lesson>>(view.Model);
        var ids = model.Select(x => x.Id).ToList();

        Assert.Equal(new[] { lesson1.Id, lesson2.Id, lesson3.Id }, ids);
    }

    [Fact]
    public async Task Index_ProgressForUser_IgnoresUnpublishedLessons()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        var lesson1 = AddLesson(db, category.Id, 1, true, "Урок 1");
        var lesson2 = AddLesson(db, category.Id, 2, false, "Чернетка");
        var lesson3 = AddLesson(db, category.Id, 3, true, "Урок 3");

        db.UserLessonProgresses.Add(new UserLessonProgress(UserId, lesson1.Id));
        db.UserLessonProgresses.Add(new UserLessonProgress(UserId, lesson2.Id));
        db.UserLessonProgresses.Add(new UserLessonProgress(UserId, lesson3.Id));
        await db.SaveChangesAsync();

        var controller = CreateController(db, isAdmin: false);

        await controller.Index();

        var openedLessons = Assert.IsType<List<int>>(controller.ViewBag.OpenedLessons);

        Assert.Equal(new[] { lesson1.Id, lesson3.Id }, openedLessons);
        Assert.DoesNotContain(lesson2.Id, openedLessons);
    }

    [Fact]
    public async Task Details_Get_FirstPublishedLesson_IsAllowedForUserWithoutProgress()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        var draft = AddLesson(db, category.Id, 1, false, "Чернетка");
        var firstPublished = AddLesson(db, category.Id, 2, true, "Перший опублікований");

        var controller = CreateController(db, isAdmin: false);

        var result = await controller.Details(firstPublished.Id);

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Details_Get_LockedPublishedLesson_IsBlockedForUser()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        var lesson1 = AddLesson(db, category.Id, 1, true, "Урок 1");
        var lesson2 = AddLesson(db, category.Id, 2, true, "Урок 2");

        db.UserLessonProgresses.Add(new UserLessonProgress(UserId, lesson1.Id));
        await db.SaveChangesAsync();

        var controller = CreateController(db, isAdmin: false);

        var result = await controller.Details(lesson2.Id);

        AssertBlocked(result);
    }

    [Fact]
    public async Task Details_Get_DraftLesson_IsBlockedForUser()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        var draft = AddLesson(db, category.Id, 1, false, "Чернетка");

        var controller = CreateController(db, isAdmin: false);

        var result = await controller.Details(draft.Id);

        AssertBlocked(result);
    }

    [Fact]
    public async Task Details_Get_DraftLesson_IsAllowedForAdmin()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        var draft = AddLesson(db, category.Id, 1, false, "Чернетка");

        var controller = CreateController(db, isAdmin: true);

        var result = await controller.Details(draft.Id);

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Details_Post_LockedLesson_IsBlockedAndDoesNotCreateAttempt()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        var lesson1 = AddLesson(db, category.Id, 1, true, "Урок 1");
        var lesson2 = AddLesson(db, category.Id, 2, true, "Урок 2");

        db.UserLessonProgresses.Add(new UserLessonProgress(UserId, lesson1.Id));
        await db.SaveChangesAsync();

        var controller = CreateController(db, isAdmin: false);
        var model = BuildCorrectSubmitModel(db, lesson2.Id);

        var result = await controller.Details(model);

        AssertBlocked(result);

        var attemptsCount = await db.LessonTestAttempts
            .CountAsync(x => x.UserId == UserId && x.LessonId == lesson2.Id);

        Assert.Equal(0, attemptsCount);
    }

    [Fact]
    public async Task Details_Post_DraftLesson_IsBlockedAndDoesNotCreateAttempt()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        var draft = AddLesson(db, category.Id, 1, false, "Чернетка");

        var controller = CreateController(db, isAdmin: false);
        var model = BuildCorrectSubmitModel(db, draft.Id);

        var result = await controller.Details(model);

        AssertBlocked(result);

        var attemptsCount = await db.LessonTestAttempts
            .CountAsync(x => x.UserId == UserId && x.LessonId == draft.Id);

        Assert.Equal(0, attemptsCount);
    }

    [Fact]
    public async Task Details_Post_PassedLesson_OpensNextPublishedLessonAndSkipsDraft()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        var lesson1 = AddLesson(db, category.Id, 1, true, "Урок 1");
        var lesson2 = AddLesson(db, category.Id, 2, false, "Чернетка посередині");
        var lesson3 = AddLesson(db, category.Id, 3, true, "Урок 3");

        db.UserLessonProgresses.Add(new UserLessonProgress(UserId, lesson1.Id));
        await db.SaveChangesAsync();

        var controller = CreateController(db, isAdmin: false);
        var model = BuildCorrectSubmitModel(db, lesson1.Id);

        await controller.Details(model);

        bool openedDraft = await db.UserLessonProgresses
            .AnyAsync(x => x.UserId == UserId && x.LessonId == lesson2.Id);

        bool openedNextPublished = await db.UserLessonProgresses
            .AnyAsync(x => x.UserId == UserId && x.LessonId == lesson3.Id);

        Assert.False(openedDraft);
        Assert.True(openedNextPublished);
    }

    [Fact]
    public async Task Details_Post_FailedLesson_DoesNotOpenNextLesson()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        var lesson1 = AddLesson(db, category.Id, 1, true, "Урок 1");
        var lesson2 = AddLesson(db, category.Id, 2, true, "Урок 2");

        db.UserLessonProgresses.Add(new UserLessonProgress(UserId, lesson1.Id));
        await db.SaveChangesAsync();

        var controller = CreateController(db, isAdmin: false);
        var model = BuildWrongSubmitModel(db, lesson1.Id);

        await controller.Details(model);

        bool openedNextLesson = await db.UserLessonProgresses
            .AnyAsync(x => x.UserId == UserId && x.LessonId == lesson2.Id);

        Assert.False(openedNextLesson);
    }

    [Fact]
    public async Task Create_InsertLessonInMiddle_ShiftsNumbersCorrectly()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        AddLesson(db, category.Id, 1, false, "Урок 1");
        AddLesson(db, category.Id, 2, false, "Урок 2");
        AddLesson(db, category.Id, 3, false, "Урок 3");

        var controller = CreateController(db, isAdmin: true);

        var model = new CreateLessonViewModel
        {
            Number = 2,
            Title = "Новий урок",
            Description = "Опис",
            Content = "Контент",
            CategoryId = category.Id,
            CreatedAt = new DateOnly(2026, 1, 1),
            Questions = new List<QuestionInputModel>
            {
                CreateQuestionInput()
            }
        };

        await controller.Create(model);

        var ordered = await db.Lessons
            .OrderBy(x => x.Number)
            .Select(x => $"{x.Number}:{x.Title}")
            .ToListAsync();

        Assert.Equal(
            new[]
            {
                "1:Урок 1",
                "2:Новий урок",
                "3:Урок 2",
                "4:Урок 3"
            },
            ordered);
    }

    [Fact]
    public async Task Edit_MoveLessonFromThirdToFirst_ShiftsNumbersCorrectly()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        AddLesson(db, category.Id, 1, false, "Урок 1");
        AddLesson(db, category.Id, 2, false, "Урок 2");
        var lesson3 = AddLesson(db, category.Id, 3, false, "Урок 3");

        var controller = CreateController(db, isAdmin: true);

        var model = new EditLessonViewModel
        {
            Id = lesson3.Id,
            Number = 1,
            Title = "Урок 3",
            Description = "Опис",
            Content = "Контент",
            CategoryId = category.Id,
            IsPublished = false,
            Questions = new List<QuestionEditModel>
            {
                CreateQuestionEdit()
            }
        };

        await controller.Edit(model);

        var ordered = await db.Lessons
            .OrderBy(x => x.Number)
            .Select(x => $"{x.Number}:{x.Title}")
            .ToListAsync();

        Assert.Equal(
            new[]
            {
                "1:Урок 3",
                "2:Урок 1",
                "3:Урок 2"
            },
            ordered);
    }

    [Fact]
    public async Task DeleteConfirmed_DeleteMiddleLesson_ShiftsNumbersCorrectly()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        AddLesson(db, category.Id, 1, false, "Урок 1");
        var lesson2 = AddLesson(db, category.Id, 2, false, "Урок 2");
        AddLesson(db, category.Id, 3, false, "Урок 3");

        var controller = CreateController(db, isAdmin: true);

        await controller.DeleteConfirmed(lesson2.Id);

        var ordered = await db.Lessons
            .OrderBy(x => x.Number)
            .Select(x => $"{x.Number}:{x.Title}")
            .ToListAsync();

        Assert.Equal(
            new[]
            {
                "1:Урок 1",
                "2:Урок 3"
            },
            ordered);
    }

    [Fact]
    public async Task Edit_PublishedLessonWithoutUnpublish_DoesNotChangeLesson()
    {
        using var db = CreateDbContext();
        var category = AddCategory(db);

        var lesson = AddLesson(db, category.Id, 1, true, "Стара назва");

        var controller = CreateController(db, isAdmin: true);

        var model = new EditLessonViewModel
        {
            Id = lesson.Id,
            Number = 1,
            Title = "Нова назва",
            Description = "Новий опис",
            Content = "Новий контент",
            CategoryId = category.Id,
            IsPublished = true,
            Questions = new List<QuestionEditModel>
            {
                CreateQuestionEdit()
            }
        };

        var result = await controller.Edit(model);

        Assert.IsType<ViewResult>(result);

        var after = await db.Lessons.FindAsync(lesson.Id);

        Assert.NotNull(after);
        Assert.Equal("Стара назва", after!.Title);
        Assert.True(after.IsPublished);
    }

    [Fact]
    public void Lesson_Number_HasUniqueIndex()
    {
        using var db = CreateDbContext();

        var entityType = db.Model.FindEntityType(typeof(Lesson));

        Assert.NotNull(entityType);

        var index = entityType!
            .GetIndexes()
            .FirstOrDefault(i =>
                i.Properties.Count == 1 &&
                i.Properties[0].Name == nameof(Lesson.Number));

        Assert.NotNull(index);
        Assert.True(index!.IsUnique);
    }

    [Fact]
    public void DeleteConfirmed_PostAction_MustRequireAdminRole()
    {
        var method = typeof(LessonsController)
            .GetMethod(nameof(LessonsController.DeleteConfirmed));

        Assert.NotNull(method);

        var hasAdminAuthorize = method!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Any(x => x.Roles == RoleNames.Admin);

        Assert.True(
            hasAdminAuthorize,
            "POST DeleteConfirmed має мати [Authorize(Roles = RoleNames.Admin)]. Зараз у архіві цей метод має лише [ValidateAntiForgeryToken], а клас має просто [Authorize].");
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static LessonCategory AddCategory(ApplicationDbContext db)
    {
        var category = new LessonCategory("Основи C#");
        db.LessonCategories.Add(category);
        db.SaveChanges();

        return category;
    }

    private static Lesson AddLesson(
    ApplicationDbContext db,
    int categoryId,
    int number,
    bool publish,
    string title)
    {
        var lesson = new Lesson(
            number,
            title,
            "Опис",
            "Контент",
            categoryId,
            new DateOnly(2026, 1, 1));

        lesson.AddQuestion(CreateValidQuestion("Питання 1?"));

        if (publish)
        {
            lesson.AddQuestion(CreateValidQuestion("Питання 2?"));
            lesson.Publish();
        }

        db.Lessons.Add(lesson);
        db.SaveChanges();

        return lesson;
    }

    private static TheoryQuestion CreateValidQuestion(string text = "Питання?")
    {
        var question = new TheoryQuestion(text, 1);

        question.AddAnswerOption("Правильно", true);
        question.AddAnswerOption("Неправильно", false);

        return question;
    }

    private static QuestionInputModel CreateQuestionInput()
    {
        return new QuestionInputModel
        {
            QuestionText = "Питання?",
            Answers = new List<AnswerInputModel>
            {
                new() { Text = "Правильно", IsCorrect = true },
                new() { Text = "Неправильно", IsCorrect = false }
            }
        };
    }

    private static List<QuestionInputModel> CreateTwoQuestionInputs()
    {
        return new List<QuestionInputModel>
    {
        CreateQuestionInput("Питання 1?"),
        CreateQuestionInput("Питання 2?")
    };
    }

    private static QuestionEditModel CreateQuestionEdit()
    {
        return new QuestionEditModel
        {
            QuestionText = "Питання?",
            Answers = new List<AnswerEditModel>
            {
                new() { Text = "Правильно", IsCorrect = true },
                new() { Text = "Неправильно", IsCorrect = false }
            }
        };
    }

    private static LessonDetailsViewModel BuildCorrectSubmitModel(
        ApplicationDbContext db,
        int lessonId)
    {
        var lesson = db.Lessons
            .Include(x => x.Questions)
            .ThenInclude(x => x.AnswerOptions)
            .First(x => x.Id == lessonId);

        return new LessonDetailsViewModel
        {
            LessonId = lesson.Id,
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
    }

    private static LessonDetailsViewModel BuildWrongSubmitModel(
        ApplicationDbContext db,
        int lessonId)
    {
        var lesson = db.Lessons
            .Include(x => x.Questions)
            .ThenInclude(x => x.AnswerOptions)
            .First(x => x.Id == lessonId);

        return new LessonDetailsViewModel
        {
            LessonId = lesson.Id,
            Questions = lesson.Questions.Select(q => new LessonDetailsViewModel.QuestionVm
            {
                QuestionId = q.Id,
                QuestionText = q.QuestionText,
                Answers = q.AnswerOptions.Select(a => new LessonDetailsViewModel.AnswerVm
                {
                    AnswerId = a.Id,
                    Text = a.Text,
                    IsSelected = !a.IsCorrect
                }).ToList()
            }).ToList()
        };
    }

    private static LessonsController CreateController(
        ApplicationDbContext db,
        bool isAdmin)
    {
        var userManager = CreateUserManager();

        var controller = new LessonsController(db, userManager);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = CreatePrincipal(isAdmin)
            }
        };

        return controller;
    }

    private static ClaimsPrincipal CreatePrincipal(bool isAdmin)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, isAdmin ? AdminId : UserId),
            new(ClaimTypes.Name, isAdmin ? "admin@eweb.com" : "user@eweb.com")
        };

        if (isAdmin)
            claims.Add(new Claim(ClaimTypes.Role, RoleNames.Admin));

        var identity = new ClaimsIdentity(claims, "TestAuth");

        return new ClaimsPrincipal(identity);
    }

    private static UserManager<ApplicationUser> CreateUserManager()
    {
        return new UserManager<ApplicationUser>(
            new TestUserStore(),
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            new LoggerFactory().CreateLogger<UserManager<ApplicationUser>>());
    }

    private static void AssertBlocked(IActionResult result)
    {
        bool blocked =
            result is RedirectToActionResult redirect && redirect.ActionName == "Index"
            || result is NotFoundResult
            || result is ForbidResult
            || result is ChallengeResult;

        Assert.True(
            blocked,
            $"Очікувалось блокування доступу, але отримано: {result.GetType().Name}");
    }

    private static List<QuestionEditModel> CreateTwoQuestionEdits()
    {
        return new List<QuestionEditModel>
    {
        new()
        {
            QuestionText = "Питання 1?",
            Answers = new List<AnswerEditModel>
            {
                new() { Text = "Правильно", IsCorrect = true },
                new() { Text = "Неправильно", IsCorrect = false }
            }
        },
        new()
        {
            QuestionText = "Питання 2?",
            Answers = new List<AnswerEditModel>
            {
                new() { Text = "Правильно", IsCorrect = true },
                new() { Text = "Неправильно", IsCorrect = false }
            }
        }
    };
    }


    private sealed class TestUserStore : IUserStore<ApplicationUser>
    {
        public void Dispose()
        {
        }

        public Task<string> GetUserIdAsync(
            ApplicationUser user,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Id);
        }

        public Task<string?> GetUserNameAsync(
            ApplicationUser user,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(user.UserName);
        }

        public Task SetUserNameAsync(
            ApplicationUser user,
            string? userName,
            CancellationToken cancellationToken)
        {
            user.UserName = userName;
            return Task.CompletedTask;
        }

        public Task<string?> GetNormalizedUserNameAsync(
            ApplicationUser user,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(user.NormalizedUserName);
        }

        public Task SetNormalizedUserNameAsync(
            ApplicationUser user,
            string? normalizedName,
            CancellationToken cancellationToken)
        {
            user.NormalizedUserName = normalizedName;
            return Task.CompletedTask;
        }

        public Task<IdentityResult> CreateAsync(
            ApplicationUser user,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<IdentityResult> UpdateAsync(
            ApplicationUser user,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<IdentityResult> DeleteAsync(
            ApplicationUser user,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<ApplicationUser?> FindByIdAsync(
            string userId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<ApplicationUser?>(new ApplicationUser
            {
                Id = userId,
                UserName = "test@eweb.com"
            });
        }

        public Task<ApplicationUser?> FindByNameAsync(
            string normalizedUserName,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<ApplicationUser?>(new ApplicationUser
            {
                Id = UserId,
                UserName = normalizedUserName
            });
        }
    }
}