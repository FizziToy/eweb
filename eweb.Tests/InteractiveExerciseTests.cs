using eweb.Domain.Entities.Exercises;

namespace eweb.Tests
{
    public class InteractiveExerciseTests
    {
        [Fact]
        public void LessThanThreeTasks()
        {
            var exercise = new InteractiveExercise(1, "Title", "Desc", 1);

            exercise.AddTask(new ExerciseTask(
                ExerciseType.MultipleChoice,
                "Q1",
                "{}",
                1,
                1));

            exercise.AddTask(new ExerciseTask(
                ExerciseType.MultipleChoice,
                "Q2",
                "{}",
                1,
                2));

            Assert.Throws<InvalidOperationException>(() =>
                exercise.EnsureCanBePublished());
        }

        [Fact]
        public void MoreThanFiveTasks()
        {
            var exercise = new InteractiveExercise(1, "Title", "Desc", 1);

            for (int i = 1; i <= 6; i++)
            {
                exercise.AddTask(new ExerciseTask(
                    ExerciseType.MultipleChoice,
                    $"Q{i}",
                    "{}",
                    1,
                    i));
            }

            Assert.Throws<InvalidOperationException>(() =>
                exercise.EnsureCanBePublished());
        }

        [Fact]
        public void BetweenThreeAndFiveTasks()
        {
            var exercise = new InteractiveExercise(1, "Title", "Desc", 1);

            for (int i = 1; i <= 3; i++)
            {
                exercise.AddTask(new ExerciseTask(
                    ExerciseType.MultipleChoice,
                    $"Q{i}",
                    "{}",
                    1,
                    i));
            }

            var exception = Record.Exception(() =>
                exercise.EnsureCanBePublished());

            Assert.Null(exception);
        }

        [Fact]
        public void Published()
        {
            var exercise = new InteractiveExercise(1, "Title", "Desc", 1);

            exercise.Publish();

            Assert.Throws<InvalidOperationException>(() =>
                exercise.EnsureCanBeEdited());
        }

        [Fact]
        public void TwoOrMore()
        {
            Assert.Throws<InvalidOperationException>(() =>
                InteractiveExercise.EnsureLessonExerciseLimit(2));
        }

        [Fact]
        public void LessThanTwo()
        {
            var exception = Record.Exception(() =>
                InteractiveExercise.EnsureLessonExerciseLimit(1));

            Assert.Null(exception);
        }
    }
}
