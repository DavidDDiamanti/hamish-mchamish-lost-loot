using UnityEngine;
using UnityEngine.UI;

// LevelStartUI: panel shown at the start of each level before play begins.
// Displayed by GameManager.ForceFreezeForStartGate(); hidden when the player clicks the
// start button which calls GameManager.PressStartLevel().
public class LevelStartUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;

    [Header("Button (child)")]
    [SerializeField] private Button startButton;

    private GameManager gameManager;

    private void Awake()
    {
        gameManager = FindObjectOfType<GameManager>(true);
        if (gameManager == null)
        {
            Debug.LogError("LevelStartUI: No GameManager found in scene. Start button will not be wired.");
        }

        if (startButton == null)
            startButton = GetComponentInChildren<Button>(true);

        if (startButton == null)
        {
            Debug.LogError("LevelStartUI: No Button found in children (or not assigned).");
        }
        else
        {
            startButton.onClick.RemoveAllListeners();

            if (gameManager != null)
                startButton.onClick.AddListener(gameManager.PressStartLevel);
        }

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void Show()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public bool IsShowing()
    {
        return panelRoot != null && panelRoot.activeSelf;
    }
}
