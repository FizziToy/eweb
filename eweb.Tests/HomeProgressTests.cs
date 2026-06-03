using System.Security.Claims;
using eweb.Domain.Entities;
using eweb.Domain.Entities.Progress;
using eweb.Domain.Services;
using eweb.Infrastructure.Data;
using eweb.Web.Controllers;
using eweb.Web.Models.Home;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eweb.Tests;

public class HomeProgressTests
{
    private const string UserId = "user-1";

    [Fact]
    public async Task Index_IgnoresUnpublishedLessonsAndQuestionsInProgress()
    {
        using var db = CreateDbContext();

        var category = new LessonCategory("Основи");
        db.LessonCategories.Add(category);
        await db.SaveChangesAsync();

        var publishedLesson = CreateLesson(
            category.Id,
            number: 1,
            publish: true,
            title: "Опублікований урок");

        var draftLesson = CreateLesson(
            category.Id,
            number: 2,
            publish: false,
            title: "Чернетка");

        db.Lessons.AddRange(publishedLesson, draftLesson);
        await db.SaveChangesAsync();

        var publishedQuestions = await db.TheoryQuestions
            .Where(q => q.LessonId == publishedLesson.Id)
            .OrderBy(q => q.Id)
            .ToListAsync();

        var draftQuestions = await db.TheoryQuestions
            .Where(q => q.LessonId == draftLesson.Id)
            .OrderBy(q => q.Id)
            .ToListAsync();

        db.UserLessonProgresses.Add(new UserLessonProgress(UserId, publishedLesson.Id));
        db.UserLessonProgresses.Add(new UserLessonProgress(UserId, draftLesson.Id));

        db.UserQuestionProgresses.Add(new UserQuestionProgress(UserId, publishedQuestions[0].Id));
        db.UserQuestionProgresses.Add(new UserQuestionProgress(UserId, draftQuestions[0].Id));

        await db.SaveChangesAsync();

        var controller = CreateController(db);

        var result = await controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<HomeViewModel>(viewResult.Model);

        // Має рахуватися тільки:
        // відкриті уроки: 1 з 1 опублікованого = 10%
        // пройдені питання: 1 з 2 питань опублікованого уроку = 17.5%
        // вправ немає = 0%
        // Разом: 27.5
        Assert.Equal(27.5, model.ProgressPercent);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static HomeController CreateController(ApplicationDbContext db)
    {
        var controller = new HomeController(
            new LoggerFactory().CreateLogger<HomeController>(),
            db,
            new ProgressCalculator());

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = CreatePrincipal()
            }
        };

        return controller;
    }

    private static ClaimsPrincipal CreatePrincipal()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, UserId),
            new(ClaimTypes.Name, "user@eweb.com")
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");

        return new ClaimsPrincipal(identity);
    }

    private static Lesson CreateLesson(
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
        lesson.AddQuestion(CreateValidQuestion("Питання 2?"));

        if (publish)
        {
            lesson.Publish();
        }

        return lesson;
    }

    private static TheoryQuestion CreateValidQuestion(string text)
    {
        var question = new TheoryQuestion(text, 0);

        question.AddAnswerOption("Правильно", true);
        question.AddAnswerOption("Неправильно", false);

        return question;
    }
}
