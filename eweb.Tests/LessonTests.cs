using eweb.Domain.Entities;

namespace eweb.Tests;

public class LessonTests
{
    [Fact]
    public void Create_Valid_SetsFields()
    {
        var lesson = new Lesson(1, "Назва", "Опис", "Контент");

        Assert.Equal("Назва", lesson.Title);
        Assert.False(lesson.IsPublished);
    }

    [Fact]
    public void Create_TrimsText()
    {
        var lesson = new Lesson(1, "  Назва  ", "  Опис  ", "  Контент  ");

        Assert.Equal("Назва", lesson.Title);
    }

    [Fact]
    public void Publish_SetsPublished()
    {
        var lesson = BuildPublishableLesson();

        lesson.Publish();

        Assert.True(lesson.IsPublished);
    }

    [Fact]
    public void Publish_AlrPublished_Throws()
    {
        var lesson = BuildPublishableLesson();

        lesson.Publish();

        Assert.Throws<InvalidOperationException>(() =>
            lesson.Publish());
    }

    [Fact]
    public void Unpublish_SetsFalse()
    {
        var lesson = BuildPublishableLesson();

        lesson.Publish();
        lesson.Unpublish();

        Assert.False(lesson.IsPublished);
    }

    [Fact]
    public void Unpublish_NotPublished_Throws()
    {
        var lesson = BuildPublishableLesson();

        Assert.Throws<InvalidOperationException>(() =>
            lesson.Unpublish());
    }

    [Fact]
    public void AddQuestion_Valid_Adds()
    {
        var lesson = new Lesson(1, "Назва", "Опис", "Контент");

        var question = BuildValidQuestion(lesson.Id);

        lesson.AddQuestion(question);

        Assert.Single(lesson.Questions);
    }

    [Fact]
    public void AddQuestion_Published_Throws()
    {
        var lesson = BuildPublishableLesson();

        lesson.Publish();

        var question = BuildValidQuestion(lesson.Id);

        Assert.Throws<InvalidOperationException>(() =>
            lesson.AddQuestion(question));
    }

    [Fact]
    public void AddQuestion_MaxAllowed()
    {
        var lesson = new Lesson(1, "Назва", "Опис", "Контент");

        for (int i = 0; i < 10; i++)
        {
            lesson.AddQuestion(BuildValidQuestion(lesson.Id));
        }

        Assert.Equal(10, lesson.Questions.Count);
    }

    [Fact]
    public void AddQuestion_OverLimit_Throws()
    {
        var lesson = new Lesson(1, "Назва", "Опис", "Контент");

        for (int i = 0; i < 10; i++)
        {
            lesson.AddQuestion(BuildValidQuestion(lesson.Id));
        }

        Assert.Throws<InvalidOperationException>(() =>
            lesson.AddQuestion(BuildValidQuestion(lesson.Id)));
    }

    [Fact]
    public void Update_Valid_ChangesData()
    {
        var lesson = new Lesson(1, "Стара назва", "Опис", "Контент");

        lesson.Update(2, "Нова назва", "Новий опис", "Новий контент");

        Assert.Equal("Нова назва", lesson.Title);
    }

    [Fact]
    public void Update_Published_Throws()
    {
        var lesson = BuildPublishableLesson();

        lesson.Publish();

        Assert.Throws<InvalidOperationException>(() =>
            lesson.Update(2, "Нова назва", "Опис", "Контент"));
    }

    private static Lesson BuildPublishableLesson()
    {
        var lesson = new Lesson(1, "Назва", "Опис", "Контент");

        lesson.AddQuestion(BuildValidQuestion(lesson.Id));

        return lesson;
    }

    private static TheoryQuestion BuildValidQuestion(int lessonId)
    {
        var q = new TheoryQuestion("Тестове питання?", lessonId);

        q.AddAnswerOption("Правильна", true);
        q.AddAnswerOption("Неправильна", false);

        return q;
    }

    [Fact]
    public void Publish_NoQuestions_Throws()
    {
        var lesson = new Lesson(1, "Назва", "Опис", "Контент");

        Assert.Throws<InvalidOperationException>(() =>
            lesson.Publish());
    }

    [Fact]
    public void RemoveQuestion_NotFound_Throws()
    {
        var lesson = new Lesson(1, "Назва", "Опис", "Контент");

        var q = BuildValidQuestion(lesson.Id);
        lesson.AddQuestion(q);

        Assert.Throws<InvalidOperationException>(() =>
            lesson.RemoveQuestion(999));
    }

    [Fact]
    public void RemoveQuestion_Valid_Removes()
    {
        var lesson = new Lesson(1, "Назва", "Опис", "Контент");

        var q = BuildValidQuestion(lesson.Id);

        lesson.AddQuestion(q);

        lesson.RemoveQuestion(q.Id);

        Assert.Empty(lesson.Questions);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_InvalidNumber_Throws(int number)
    {
        Assert.Throws<ArgumentException>(() =>
            new Lesson(number, "Назва", "Опис", "Контент"));
    }

    [Fact]
    public void Publish_EmptyTitle_Throws()
    {
        var lesson = new Lesson(1, "", "Опис", "Контент");

        lesson.AddQuestion(BuildValidQuestion(lesson.Id));

        Assert.Throws<InvalidOperationException>(() =>
            lesson.Publish());
    }

    [Fact]
    public void RemoveQuestion_Published_Throws()
    {
        var lesson = BuildPublishableLesson();

        lesson.Publish();

        var q = lesson.Questions.First();

        Assert.Throws<InvalidOperationException>(() =>
            lesson.RemoveQuestion(q.Id));
    }

    [Theory]
    [InlineData(50, true)]
    [InlineData(70, true)]
    [InlineData(100, true)]
    [InlineData(49.9, false)]
    [InlineData(30, false)]
    public void ResultPercent_CheckIfNextLessonShouldOpen(double percent, bool expected)
    {
        bool canOpenNextLesson = percent >= 50;

        Assert.Equal(expected, canOpenNextLesson);
    }
}