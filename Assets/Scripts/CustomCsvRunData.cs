using System;
using System.Collections.Generic;

// CustomCsvRunData: serializable data container for a custom CSV question set.
// Persisted as JSON by CustomCsvRunManager and loaded on startup.
// Each level holds exactly 10 RuntimeQuestions after validation in CreateRunFromCsvTexts().
[Serializable]
public class CustomCsvRunData
{
    public string runId;
    public string displayName;
    public string characterId;

    public List<RuntimeQuestion> level1Questions = new();
    public List<RuntimeQuestion> level2Questions = new();
    public List<RuntimeQuestion> level3Questions = new();
}
