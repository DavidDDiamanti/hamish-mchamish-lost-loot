using System;
using System.Collections.Generic;

// RuntimeQuestion: serializable plain-C# question used by CustomCsvRunManager and CustomCsvRunData.
// Unlike QuestionData (a ScriptableObject), this can be serialized to JSON for disk persistence.
// Converted to QuestionData at spawn time via RuntimeQuestionConverterX.ToQuestionData().
[Serializable]
public class RuntimeQuestion
{
    public string questionText;
    public List<string> answers = new List<string>(4);
    public int correctAnswerIndex;
    public string explanation;
}
