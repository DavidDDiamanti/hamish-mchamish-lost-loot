using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// CustomCsvRunManager: DontDestroyOnLoad singleton that manages custom CSV question sets.
// CSV format: one header row (optional), then rows of: question, answer0, answer1, answer2, answer3.
// Answer 0 is always the correct answer in the source CSV; ParseCsv shuffles answers and updates
// correctAnswerIndex so the correct answer's position is randomised per-run, not per-question.
// CreateRunFromCsvTexts validates that each CSV yields exactly 10 questions.
// The run is persisted to JSON in Application.persistentDataPath and reloaded on next launch.
// EntitySpawner reads questions via GetQuestionsForLevel() when IsBaseProfile is false.
public class CustomCsvRunManager : MonoBehaviour
{
    public static CustomCsvRunManager Instance { get; private set; }

    [SerializeField] private string customCharacterId = "CustomCharacter";

    private string SavePath => Path.Combine(Application.persistentDataPath, "custom_csv_run.json");

    public CustomCsvRunData CurrentRun { get; private set; }

    public bool HasCustomRun =>
        CurrentRun != null &&
        CurrentRun.level1Questions != null &&
        CurrentRun.level2Questions != null &&
        CurrentRun.level3Questions != null &&
        CurrentRun.level1Questions.Count > 0 &&
        CurrentRun.level2Questions.Count > 0 &&
        CurrentRun.level3Questions.Count > 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadRun();
    }

    public bool CreateRunFromCsvTexts(string level1Csv, string level2Csv, string level3Csv, string displayName = "Custom CSV Run")
    {
        try
        {
            List<RuntimeQuestion> q1 = ParseCsv(level1Csv);
            List<RuntimeQuestion> q2 = ParseCsv(level2Csv);
            List<RuntimeQuestion> q3 = ParseCsv(level3Csv);

            if (q1.Count != 10 || q2.Count != 10 || q3.Count != 10)
            {
                Debug.LogError("Each CSV must contain exactly 10 valid questions.");
                return false;
            }

            CurrentRun = new CustomCsvRunData
            {
                runId = "CUSTOM_CSV_RUN",
                displayName = displayName,
                characterId = customCharacterId,
                level1Questions = q1,
                level2Questions = q2,
                level3Questions = q3
            };

            SaveRun();
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to create CSV run: {ex.Message}");
            return false;
        }
    }

    public List<RuntimeQuestion> GetQuestionsForLevel(int level)
    {
        if (CurrentRun == null) return null;

        return level switch
        {
            1 => CurrentRun.level1Questions,
            2 => CurrentRun.level2Questions,
            3 => CurrentRun.level3Questions,
            _ => null
        };
    }

    public void DeleteCustomRun()
    {
        CurrentRun = null;

        if (File.Exists(SavePath))
            File.Delete(SavePath);
    }

    private void SaveRun()
    {
        if (CurrentRun == null) return;
        File.WriteAllText(SavePath, JsonUtility.ToJson(CurrentRun, true));
    }

    private void LoadRun()
    {
        if (!File.Exists(SavePath))
        {
            CurrentRun = null;
            return;
        }

        CurrentRun = JsonUtility.FromJson<CustomCsvRunData>(File.ReadAllText(SavePath));
    }

    // Parses a CSV string into RuntimeQuestions. Skips a header row if the first column contains
    // "question". Requires at least 5 columns per row (question + 4 answers).
    // Answer 0 is treated as correct; ShuffleAnswers randomises the order and updates the index.
    private List<RuntimeQuestion> ParseCsv(string csvText)
    {
        List<RuntimeQuestion> result = new();
        string[] lines = csvText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < lines.Length; i++)
        {
            List<string> cols = SplitCsvLine(lines[i]);

            if (i == 0 && cols.Count >= 5 && cols[0].ToLower().Contains("question"))
                continue;

            if (cols.Count < 5)
                continue;

            RuntimeQuestion q = new RuntimeQuestion
            {
                questionText = cols[0].Trim(),
                correctAnswerIndex = 0,
                explanation = ""
            };

            q.answers.Add(cols[1].Trim());
            q.answers.Add(cols[2].Trim());
            q.answers.Add(cols[3].Trim());
            q.answers.Add(cols[4].Trim());

            if (string.IsNullOrWhiteSpace(q.questionText))
                continue;

            ShuffleAnswers(q);
            result.Add(q);
        }

        return result;
    }

    // Shuffles the answers list in-place and updates correctAnswerIndex to track the correct answer.
    private void ShuffleAnswers(RuntimeQuestion q)
    {
        string correct = q.answers[q.correctAnswerIndex];

        for (int i = 0; i < q.answers.Count; i++)
        {
            int r = UnityEngine.Random.Range(i, q.answers.Count);
            (q.answers[i], q.answers[r]) = (q.answers[r], q.answers[i]);
        }

        q.correctAnswerIndex = q.answers.IndexOf(correct);
    }

    // RFC 4180-compliant CSV field splitter. Handles quoted fields and escaped double-quotes ("").
    private List<string> SplitCsvLine(string line)
    {
        List<string> fields = new();
        bool inQuotes = false;
        string current = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current += '"';
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(current);
                current = "";
            }
            else
            {
                current += c;
            }
        }

        fields.Add(current);
        return fields;
    }
}
