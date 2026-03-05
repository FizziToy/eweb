using eweb.Domain.Entities;

namespace eweb.Tests;

public class AnswerOptionTests
{
    [Fact]
    public void Create_ValidText_Succeeds()
    {
        var answer = new AnswerOption("Правильна відповідь", true);

        Assert.Equal("Правильна відповідь", answer.Text);
        Assert.True(answer.IsCorrect);
    }

    [Fact]
    public void Create_ValidTextFalse_Succeeds()
    {
        var answer = new AnswerOption("Неправильна відповідь", false);

        Assert.Equal("Неправильна відповідь", answer.Text);
        Assert.False(answer.IsCorrect);
    }

    [Fact]
    public void Create_TrimsWhitespace()
    {
        var answer = new AnswerOption("   Текст відповіді   ", false);

        Assert.Equal("Текст відповіді", answer.Text);
    }

    [Fact]
    public void Create_TrimsTabsAndSpaces()
    {
        var answer = new AnswerOption(" \t  Відповідь\t ", true);

        Assert.Equal("Відповідь", answer.Text);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Create_EmptyOrWhitespaceText_Throws(string text)
    {
        Assert.Throws<ArgumentException>(() =>
            new AnswerOption(text, false));
    }

    [Fact]
    public void Create_NullText_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new AnswerOption(null!, false));
    }

    [Fact]
    public void Create_IsCorrectTrue_SetsCorrectly()
    {
        var answer = new AnswerOption("Вірна", true);

        Assert.True(answer.IsCorrect);
    }

    [Fact]
    public void Create_IsCorrectFalse_SetsCorrectly()
    {
        var answer = new AnswerOption("Невірна", false);

        Assert.False(answer.IsCorrect);
    }

    [Fact]
    public void Create_LongText_Succeeds()
    {
        var text = new string('A', 500);

        var answer = new AnswerOption(text, true);

        Assert.Equal(text, answer.Text);
    }

    [Fact]
    public void Create_UnicodeText_Succeeds()
    {
        var answer = new AnswerOption("Змінна = variable", true);

        Assert.Equal("Змінна = variable", answer.Text);
    }

    [Fact]
    public void Create_UnicodeTrim_Succeeds()
    {
        var a = new AnswerOption("   змінна = variable   ", true);

        Assert.Equal("змінна = variable", a.Text);
    }

    [Fact]
    public void Create_VeryLongText_Succeeds()
    {
        var text = new string('A', 2000);

        var a = new AnswerOption(text, false);

        Assert.Equal(text, a.Text);
    }
}