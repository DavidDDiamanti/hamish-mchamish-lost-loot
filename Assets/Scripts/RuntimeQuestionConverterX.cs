using UnityEngine;

// RuntimeQuestionConverterX: converts a RuntimeQuestion (plain C# / JSON-serializable) into a
// QuestionData ScriptableObject instance for use by ChestBehaviour and QuizUI.
// Called by EntitySpawner.BuildQuestionSelection() when the active profile is a custom CSV profile.
// Note: QuestionProvider.cs contains an identical converter class (RuntimeQuestionConverter);
// only RuntimeQuestionConverterX is actively used; QuestionProvider is a duplicate.
public static class RuntimeQuestionConverterX
{
    public static QuestionData ToQuestionData(RuntimeQuestion rq)
    {
        QuestionData qd = ScriptableObject.CreateInstance<QuestionData>();

        qd.questionText = rq.questionText;
        qd.correctAnswerIndex = rq.correctAnswerIndex;
        qd.explanation = rq.explanation;
        qd.answers = new string[4];

        for (int i = 0; i < 4; i++)
        {
            qd.answers[i] = i < rq.answers.Count ? rq.answers[i] : "";
        }

        return qd;
    }
}
