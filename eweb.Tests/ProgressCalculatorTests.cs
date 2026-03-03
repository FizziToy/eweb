using eweb.Domain.Services;

namespace eweb.Tests
{
    public class ProgressCalculatorTests
    {
        [Fact]
        public void CorrectWeightedProgress()
        {
            var calculator = new ProgressCalculator();

            var result = calculator.Calculate(
                openedLessons: 2,
                totalLessons: 4,
                completedQuestions: 5,
                totalQuestions: 10,
                completedTasks: 8,
                totalTasks: 10);

            Assert.Equal(66.5, result);
        }

        [Fact]
        public void AllTotalsAreZero()
        {
            var calculator = new ProgressCalculator();

            var result = calculator.Calculate(
                openedLessons: 0,
                totalLessons: 0,
                completedQuestions: 0,
                totalQuestions: 0,
                completedTasks: 0,
                totalTasks: 0);

            Assert.Equal(0, result);
        }

        [Fact]
        public void ShouldRound()
        {
            var calculator = new ProgressCalculator();

            var result = calculator.Calculate(
                openedLessons: 1,
                totalLessons: 3,
                completedQuestions: 1,
                totalQuestions: 3,
                completedTasks: 1,
                totalTasks: 3);

            Assert.Equal(Math.Round(result, 2), result);
        }

        [Fact]
        public void OthersAreZero()
        {
            var calculator = new ProgressCalculator();

            var result = calculator.Calculate(
                openedLessons: 2,
                totalLessons: 4,
                completedQuestions: 0,
                totalQuestions: 0,
                completedTasks: 0,
                totalTasks: 0);

            Assert.Equal(5, result);
        }

        [Fact]
        public void GreaterThanTotal()
        {
            var calculator = new ProgressCalculator();

            var result = calculator.Calculate(
                openedLessons: 10,
                totalLessons: 5,
                completedQuestions: 20,
                totalQuestions: 10,
                completedTasks: 15,
                totalTasks: 5);

            Assert.True(result <= 100);
        }

        [Fact]
        public void NegativeValue()
        {
            var calculator = new ProgressCalculator();

            var result = calculator.Calculate(
                openedLessons: -1,
                totalLessons: 5,
                completedQuestions: 0,
                totalQuestions: 10,
                completedTasks: 0,
                totalTasks: 10);

            Assert.True(result >= 0);
        }
    }
}
