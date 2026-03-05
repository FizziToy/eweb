using eweb.Domain.Entities;

namespace eweb.Tests;

public class TheoryQuestionTests
{
    [Fact]
    public void Create_ValidText_Succeeds()
    {
        var q = new TheoryQuestion("Що таке змінна?", 1);

        Assert.Equal("Що таке змінна?", q.QuestionText);
        Assert.Equal(1, q.LessonId);
    }

    [Fact]
    public void Create_TrimsWhitespace()
    {
        var q = new TheoryQuestion("   Питання   ", 1);

        Assert.Equal("Питання", q.QuestionText);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_EmptyOrWhitespace_Throws(string text)
    {
        Assert.Throws<ArgumentException>(() =>
            new TheoryQuestion(text, 1));
    }

    [Fact]
    public void AddAnswerOption_ValidAnswer_Succeeds()
    {
        var q = new TheoryQuestion("Питання?", 1);

        q.AddAnswerOption("Відповідь", true);

        Assert.Single(q.AnswerOptions);
        Assert.True(q.AnswerOptions.First().IsCorrect);
    }

    [Fact]
    public void AddAnswerOption_MultipleAnswers_Succeeds()
    {
        var q = new TheoryQuestion("Питання?", 1);

        q.AddAnswerOption("Правильна", true);
        q.AddAnswerOption("Неправильна", false);

        Assert.Equal(2, q.AnswerOptions.Count);
    }

    [Fact]
    public void AddAnswerOption_NineAnswers_Succeeds()
    {
        var q = new TheoryQuestion("Питання?", 1);

        for (int i = 0; i < 9; i++)
            q.AddAnswerOption($"Відповідь {i}", i == 0);

        Assert.Equal(9, q.AnswerOptions.Count);
    }

    [Fact]
    public void AddAnswerOption_TenthAnswer_Throws()
    {
        var q = new TheoryQuestion("Питання?", 1);

        for (int i = 0; i < 9; i++)
            q.AddAnswerOption($"Відповідь {i}", i == 0);

        Assert.Throws<InvalidOperationException>(() =>
            q.AddAnswerOption("Десята відповідь", false));
    }

    [Fact]
    public void Validate_TwoAnswersOneCorrect_Passes()
    {
        var q = BuildValidQuestion();

        var ex = Record.Exception(() => q.Validate());

        Assert.Null(ex);
    }

    [Fact]
    public void Validate_OnlyOneAnswer_Throws()
    {
        var q = new TheoryQuestion("Питання?", 1);

        q.AddAnswerOption("Одна відповідь", true);

        Assert.Throws<InvalidOperationException>(() =>
            q.Validate());
    }

    [Fact]
    public void Validate_NoCorrectAnswers_Throws()
    {
        var q = new TheoryQuestion("Питання?", 1);

        q.AddAnswerOption("Неправильна", false);
        q.AddAnswerOption("Неправильна", false);

        Assert.Throws<InvalidOperationException>(() =>
            q.Validate());
    }

    [Fact]
    public void Validate_OneCorrectAnswer_Passes()
    {
        var q = new TheoryQuestion("Питання?", 1);

        q.AddAnswerOption("Правильна", true);
        q.AddAnswerOption("Неправильна", false);

        var ex = Record.Exception(() => q.Validate());

        Assert.Null(ex);
    }

    [Fact]
    public void Validate_ThreeCorrectAnswers_Passes()
    {
        var q = new TheoryQuestion("Питання?", 1);

        q.AddAnswerOption("Правильна 1", true);
        q.AddAnswerOption("Правильна 2", true);
        q.AddAnswerOption("Правильна 3", true);
        q.AddAnswerOption("Неправильна", false);

        var ex = Record.Exception(() => q.Validate());

        Assert.Null(ex);
    }

    [Fact]
    public void Validate_FourCorrectAnswers_Throws()
    {
        var q = new TheoryQuestion("Питання?", 1);

        q.AddAnswerOption("1", true);
        q.AddAnswerOption("2", true);
        q.AddAnswerOption("3", true);
        q.AddAnswerOption("4", true);
        q.AddAnswerOption("5", false);

        Assert.Throws<InvalidOperationException>(() =>
            q.Validate());
    }

    [Fact]
    public void Validate_AllAnswersCorrect_Throws()
    {
        var q = new TheoryQuestion("Питання?", 1);

        q.AddAnswerOption("1", true);
        q.AddAnswerOption("2", true);

        Assert.Throws<InvalidOperationException>(() =>
            q.Validate());
    }

    [Fact]
    public void RemoveAnswerOption_ExistingAnswer_RemovesIt()
    {
        var q = BuildValidQuestion();

        var id = q.AnswerOptions.First().Id;

        Assert.Throws<InvalidOperationException>(() =>
            q.RemoveAnswerOption(id));
    }

    [Fact]
    public void RemoveAnswerOption_NonExistingId_Throws()
    {
        var q = BuildValidQuestion();

        Assert.Throws<InvalidOperationException>(() =>
            q.RemoveAnswerOption(999));
    }

    private static TheoryQuestion BuildValidQuestion()
    {
        var q = new TheoryQuestion("Тестове питання?", 1);

        q.AddAnswerOption("Правильна", true);
        q.AddAnswerOption("Неправильна", false);

        return q;
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AddAnswerOption_EmptyText_Throws(string text)
    {
        var q = new TheoryQuestion("Питання?", 1);

        Assert.Throws<ArgumentException>(() =>
            q.AddAnswerOption(text, false));
    }

    [Fact]
    public void Validate_MaxAnswers_Passes()
    {
        var q = new TheoryQuestion("Питання?", 1);

        for (int i = 0; i < 8; i++)
            q.AddAnswerOption($"Неправильна {i}", false);

        q.AddAnswerOption("Правильна", true);

        var ex = Record.Exception(() => q.Validate());

        Assert.Null(ex);
    }

    [Fact]
    public void RemoveAnswerOption_LastCorrect_Throws()
    {
        var q = new TheoryQuestion("Питання?", 1);

        q.AddAnswerOption("Правильна", true);
        q.AddAnswerOption("Неправильна", false);

        var correct = q.AnswerOptions.First(a => a.IsCorrect);

        Assert.Throws<InvalidOperationException>(() =>
            q.RemoveAnswerOption(correct.Id));
    }
}