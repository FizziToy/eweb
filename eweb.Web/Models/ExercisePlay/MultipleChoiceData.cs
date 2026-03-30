namespace eweb.Web.Models.ExercisePlay
{
    public class MultipleChoiceData
    {
        public List<string> Options { get; set; } = new();
        public List<int> CorrectIndexes { get; set; } = new();
    }
}
