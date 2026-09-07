using System.Collections.Generic;
using UnityEngine;

// QuestionBank: ScriptableObject holding a named list of QuestionData assets for one level.
// Assigned per-level in GameManager and read by EntitySpawner.BuildQuestionSelection()
// when the active profile is the base profile.
[CreateAssetMenu(fileName = "QuestionBank", menuName = "Quiz/Question Bank")]
public class QuestionBank : ScriptableObject
{
    public string bankName;
    public List<QuestionData> questions = new List<QuestionData>();
}
