namespace eweb.Web.Models.ExercisePlay
{
    public class MultipleChoiceOption
    {
        public string Text { get; set; } = "";
        public bool IsCorrect { get; set; }
    }

    public class MultipleChoiceData
    {
        public List<MultipleChoiceOption> Options { get; set; } = new();
    }
}
