using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// QuizUI: modal quiz panel shown when a player opens a fully-dug chest.
// Answers are shuffled via shuffledAnswerIndices so the correct answer appears in a random button.
// Buttons are disabled and faded in over buttonFadeInDuration to prevent accidental instant picks.
// Text breathing and per-button wobble animations run each Update while the panel is active.
// ShowQuestion() is called by ChestBehaviour with onCorrect/onWrong callbacks.
// Choose() resolves the selection and invokes the appropriate callback, then hides the panel.
public class QuizUI : MonoBehaviour
{
    [SerializeField] private PlayerUI playerUI;

    [Header("UI References")]
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private TMP_Text[] answerTexts;   // size 4
    [SerializeField] private Button[] buttons;         // size 4

    [Header("Text Breathing")]
    [SerializeField] private float textBreathSpeed = 2f;
    [SerializeField] private float textBreathAmount = 0.06f;

    [Header("Button Wobble")]
    [SerializeField] private float buttonWobbleSpeed = 2f;
    [SerializeField] private float buttonWobbleAngle = 4f;

    [Header("Button Intro")]
    [SerializeField] private float buttonFadeInDuration = 1f;

    private QuestionData currentQuestion;
    private int correctAnswerIndex;
    private Action onCorrect;
    private Action onWrong;

    private RectTransform questionRect;
    private RectTransform[] buttonRects;

    private Vector3 questionBaseScale;
    private Vector3[] buttonBaseScales;
    private Quaternion[] buttonBaseRots;

    private float[] buttonPhases;
    private float questionPhase;

    private CanvasGroup[] buttonCanvasGroups;
    private Coroutine buttonIntroRoutine;
    private int[] shuffledAnswerIndices = new int[4];
    private GameManager gameManager;

    private void Awake()
    {
        gameManager = FindObjectOfType<GameManager>();

        if (rootPanel == null) rootPanel = gameObject;
        rootPanel.SetActive(false);

        questionRect = questionText != null ? questionText.rectTransform : null;
        if (questionRect != null) questionBaseScale = questionRect.localScale;

        if (buttons == null || buttons.Length != 4)
        {
            Debug.LogError("QuizUI: 'buttons' array must be size 4.");
            return;
        }

        if (answerTexts == null || answerTexts.Length != 4)
        {
            Debug.LogError("QuizUI: 'answerTexts' array must be size 4.");
            return;
        }

        buttonRects = new RectTransform[4];
        buttonBaseScales = new Vector3[4];
        buttonBaseRots = new Quaternion[4];
        buttonPhases = new float[4];
        buttonCanvasGroups = new CanvasGroup[4];

        for (int i = 0; i < 4; i++)
        {
            int answerIndex = i;
            buttons[i].onClick.AddListener(() => Choose(answerIndex));

            buttonRects[i] = buttons[i].GetComponent<RectTransform>();
            if (buttonRects[i] != null)
            {
                buttonBaseScales[i] = buttonRects[i].localScale;
                buttonBaseRots[i] = buttonRects[i].localRotation;
            }

            buttonPhases[i] = UnityEngine.Random.Range(0f, 10f);

            buttonCanvasGroups[i] = buttons[i].GetComponent<CanvasGroup>();
            if (buttonCanvasGroups[i] == null)
                buttonCanvasGroups[i] = buttons[i].gameObject.AddComponent<CanvasGroup>();
        }

        questionPhase = UnityEngine.Random.Range(0f, 10f);
    }

    private void Update()
    {
        if (rootPanel == null || !rootPanel.activeInHierarchy) return;

        AnimateTexts();
        AnimateButtons();
    }

    private void AnimateTexts()
    {
        float t = Time.unscaledTime;

        if (questionRect != null)
        {
            float s = 1f + Mathf.Sin((t + questionPhase) * textBreathSpeed) * textBreathAmount;
            questionRect.localScale = questionBaseScale * s;
        }
    }

    private void AnimateButtons()
    {
        float t = Time.unscaledTime;

        for (int i = 0; i < 4; i++)
        {
            if (buttonRects[i] == null) continue;

            float z = Mathf.Sin((t + buttonPhases[i]) * buttonWobbleSpeed) * buttonWobbleAngle;
            Quaternion targetRot = buttonBaseRots[i] * Quaternion.Euler(0f, 0f, z);

            buttonRects[i].localRotation = Quaternion.Slerp(
                buttonRects[i].localRotation,
                targetRot,
                Time.unscaledDeltaTime * 10f
            );
        }
    }

    // Shows the panel, shuffles answer positions, stores callbacks, and starts the button fade-in.
    // Buttons are not interactable until the fade-in completes to prevent accidental clicks.
    public void ShowQuestion(QuestionData question, Action onCorrectCallback, Action onWrongCallback)
    {
        if (question == null)
        {
            Debug.LogWarning("QuizUI.ShowQuestion called with null question.");
            return;
        }

        for (int i = 0; i < 4; i++)
            shuffledAnswerIndices[i] = i;

        for (int i = 0; i < 4; i++)
        {
            int j = UnityEngine.Random.Range(i, 4);
            (shuffledAnswerIndices[i], shuffledAnswerIndices[j]) =
                (shuffledAnswerIndices[j], shuffledAnswerIndices[i]);
        }

        currentQuestion = question;
        onCorrect = onCorrectCallback;
        onWrong = onWrongCallback;
        correctAnswerIndex = question.correctAnswerIndex;

        if (questionText != null)
            questionText.text = question.questionText;

        for (int i = 0; i < 4; i++)
        {
            int answerIndex = shuffledAnswerIndices[i];

            if (answerTexts[i] != null)
            {
                if (question.answers != null && answerIndex < question.answers.Length)
                    answerTexts[i].text = question.answers[answerIndex];
                else
                    answerTexts[i].text = "";
            }
        }

        if (questionRect != null)
            questionRect.localScale = questionBaseScale;

        for (int i = 0; i < 4; i++)
        {
            if (buttonRects[i] != null)
            {
                buttonRects[i].localScale = buttonBaseScales[i];
                buttonRects[i].localRotation = buttonBaseRots[i];
            }

            if (buttons[i] != null)
                buttons[i].interactable = false;

            if (buttonCanvasGroups[i] != null)
            {
                buttonCanvasGroups[i].alpha = 0f;
                buttonCanvasGroups[i].interactable = false;
                buttonCanvasGroups[i].blocksRaycasts = false;
            }
        }

        rootPanel.SetActive(true);
        gameManager.SetPlayerInQuizUI(true);
        playerUI.Hide();

        if (buttonIntroRoutine != null)
            StopCoroutine(buttonIntroRoutine);

        buttonIntroRoutine = StartCoroutine(FadeInButtonsRoutine());
    }

    private IEnumerator FadeInButtonsRoutine()
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, buttonFadeInDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(elapsed / duration);

            for (int i = 0; i < 4; i++)
            {
                if (buttonCanvasGroups[i] != null)
                    buttonCanvasGroups[i].alpha = alpha;
            }

            yield return null;
        }

        for (int i = 0; i < 4; i++)
        {
            if (buttonCanvasGroups[i] != null)
            {
                buttonCanvasGroups[i].alpha = 1f;
                buttonCanvasGroups[i].interactable = true;
                buttonCanvasGroups[i].blocksRaycasts = true;
            }

            if (buttons[i] != null)
                buttons[i].interactable = true;
        }

        buttonIntroRoutine = null;
    }

    // Resolves the chosen button against the shuffled index mapping to determine correctness.
    private void Choose(int chosenIndex)
    {
        bool isCorrect = shuffledAnswerIndices[chosenIndex] == correctAnswerIndex;

        if (isCorrect) onCorrect?.Invoke();
        else onWrong?.Invoke();

        Hide();
        playerUI.Show();
    }

    public void Hide()
    {
        if (buttonIntroRoutine != null)
        {
            StopCoroutine(buttonIntroRoutine);
            buttonIntroRoutine = null;
        }

        for (int i = 0; i < 4; i++)
        {
            if (buttons[i] != null)
                buttons[i].interactable = false;

            if (buttonCanvasGroups[i] != null)
            {
                buttonCanvasGroups[i].interactable = false;
                buttonCanvasGroups[i].blocksRaycasts = false;
            }
        }

        gameManager.SetPlayerInQuizUI(false);
        rootPanel.SetActive(false);
        currentQuestion = null;
        onCorrect = null;
        onWrong = null;
    }

    public bool IsShowing()
    {
        return rootPanel != null && rootPanel.activeInHierarchy;
    }
}
