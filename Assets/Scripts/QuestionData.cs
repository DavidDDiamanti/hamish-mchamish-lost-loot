using UnityEngine;

// QuestionData: ScriptableObject representing a single quiz question with four answers.
// correctAnswerIndex (0-3) identifies which entry in the answers array is correct.
// Used by ChestBehaviour; assigned by EntitySpawner from a QuestionBank (base profile)
// or converted from RuntimeQuestion via RuntimeQuestionConverterX (custom CSV profile).
[CreateAssetMenu(fileName = "QuestionData", menuName = "Quiz/Question")]
public class QuestionData : ScriptableObject
{
    [TextArea(2, 5)]
    public string questionText;

    public string[] answers = new string[4];
    [Range(0, 3)]
    public int correctAnswerIndex;

    [TextArea(1, 3)]
    public string explanation;
}
