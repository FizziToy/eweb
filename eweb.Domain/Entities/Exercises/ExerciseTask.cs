namespace eweb.Domain.Entities.Exercises;

public class ExerciseTask
{
    public int Id { get; private set; }

    public int ExerciseId { get; private set; }

    public ExerciseType Type { get; private set; }

    public string QuestionText { get; private set; } = null!;

    public string DataJson { get; private set; } = null!;

    public int StarsReward { get; private set; }

    public int Order { get; private set; }

    private ExerciseTask() { } // для EF

    public ExerciseTask(
        int exerciseId,
        ExerciseType type,
        string questionText,
        string dataJson,
        int starsReward,
        int order)
    {
        ExerciseId = exerciseId;
        Type = type;
        QuestionText = questionText;
        DataJson = dataJson;
        StarsReward = starsReward;
        Order = order;
    }

    public void Update(
        ExerciseType type,
        string questionText,
        string dataJson,
        int starsReward,
        int order)
    {
        Type = type;
        QuestionText = questionText;
        DataJson = dataJson;
        StarsReward = starsReward;
        Order = order;
    }
}