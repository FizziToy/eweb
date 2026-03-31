namespace eweb.Web.Models.ExercisePlay
{
    public class MatchPairsData
    {
        public List<PairItem> Pairs { get; set; } = new();
    }

    public class PairItem
    {
        public string Left { get; set; } = "";
        public string Right { get; set; } = "";
    }
}
