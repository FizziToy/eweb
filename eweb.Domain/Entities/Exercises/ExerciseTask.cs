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
        ExerciseType type,
        string questionText,
        string dataJson,
        int starsReward,
        int order)
    {
        Validate(type, questionText, dataJson, starsReward, order);

        Type = type;
        QuestionText = questionText.Trim();
        DataJson = dataJson.Trim();
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
        Validate(type, questionText, dataJson, starsReward, order);

        Type = type;
        QuestionText = questionText.Trim();
        DataJson = dataJson.Trim();
        StarsReward = starsReward;
        Order = order;
    }

    private static void Validate(
        ExerciseType type,
        string questionText,
        string dataJson,
        int starsReward,
        int order)
    {
        if (!Enum.IsDefined(type))
            throw new ArgumentException("Невідомий тип завдання.");

        if (string.IsNullOrWhiteSpace(questionText))
            throw new ArgumentException("Текст завдання не може бути порожнім.");

        if (string.IsNullOrWhiteSpace(dataJson))
            throw new ArgumentException("Дані завдання не можуть бути порожніми.");

        if (starsReward < 1 || starsReward > 2)
            throw new ArgumentException("Кількість зірок має бути від 1 до 2.");

        if (order <= 0)
            throw new ArgumentException("Порядок завдання має бути більший за 0.");
    }
}
