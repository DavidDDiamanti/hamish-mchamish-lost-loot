using System.Runtime.Serialization;
using UnityEngine;

// ChestBehaviour: two-phase interactable object.
// Phase 1 (amountDug < 1): Each Interact() call from PlayerActionArea advances dig progress
//   and plays dirt particles. The chest becomes visible at full opacity on first dig.
// Phase 2 (amountDug >= 1): Interact() triggers QuizUI with the pre-assigned QuestionData.
//   Correct answer -> OpenChest() -> GameManager.chestOpened(true, goldReward).
//   Wrong answer   -> WrongAnswer() -> GameManager.chestOpened(false, 0).
// After opening, the layer changes to Default so PlayerActionArea no longer detects it.
// QuestionData and LootTable are assigned by EntitySpawner before the scene begins.
public class ChestBehaviour : MonoBehaviour, IInteractable
{
    [SerializeField] private int goldReward = 10;

    [Header("Particles")]
    [SerializeField] private ParticleSystem dirtParticles;
    [SerializeField] private ParticleSystem unopenedChestParticles;
    [SerializeField] private LootTable lootTable;

    private QuizUI quizUI;
    private float amountDug = 0f;
    private bool opened = false;
    private SpriteRenderer sr;
    private Animator animator;
    private Player player;
    private GameManager gameManager;
    private QuestionData assignedQuestion;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        quizUI = FindObjectOfType<QuizUI>(true);
        player = FindObjectOfType<Player>(true);
        gameManager = FindObjectOfType<GameManager>(true);

        unopenedChestParticles?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        UnityEngine.Color c = sr.color;
        c.a = 0f;
        sr.color = c;
    }

    void Update()
    {
    }

    public void SetLootTable(LootTable table)
    {
        lootTable = table;
    }

    public void Interact(GameObject interactor)
    {
        if (opened) return;

        if (amountDug < 1f)
        {
            DigUp();
        }
        else
        {
            EnsureUnopenedParticles(true);

            if (quizUI == null)
            {
                Debug.LogWarning("No QuizUI found in scene!");
                return;
            }
            if (quizUI.IsShowing())
            {
                Debug.Log("QuizUI is already showing!");
                return;
            }

            if (assignedQuestion == null)
            {
                Debug.LogWarning("Chest has no assigned question.");
                return;
            }

            opened = true;
            quizUI.ShowQuestion(assignedQuestion, OpenChest, WrongAnswer);
        }
    }

    private void OpenChest()
    {
        EnsureUnopenedParticles(false);
        gameManager.chestOpened(true, goldReward);
        animator.SetBool("Opened", true);
        DropLoot();
        gameObject.layer = LayerMask.NameToLayer("Default");
    }

    private void WrongAnswer()
    {
        EnsureUnopenedParticles(false);
        gameManager.chestOpened(false, 0);
        animator.SetBool("Opened", true);
        gameObject.layer = LayerMask.NameToLayer("Default");
    }

    public void DigUp()
    {
        dirtParticles?.Play();

        if (sr.color.a == 0)
        {
            UnityEngine.Color c = sr.color;
            c.a = 1f;
            sr.color = c;
        }

        amountDug += player.GetDigPower() / 10f;
        animator.SetFloat("DigAmount", amountDug);
        Debug.Log("Dug chest: " + amountDug);

        if (amountDug >= 1f)
        {
            EnsureUnopenedParticles(true);
        }
    }

    private void EnsureUnopenedParticles(bool shouldPlay)
    {
        if (unopenedChestParticles == null) return;

        if (shouldPlay)
        {
            if (!unopenedChestParticles.isPlaying)
                unopenedChestParticles.Play();
        }
        else
        {
            if (unopenedChestParticles.isPlaying)
                unopenedChestParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void DropLoot()
    {
        if (lootTable == null) return;

        if (lootTable.TryGetDrop(out GameObject prefab))
        {
            Vector3 center = transform.position;

            Vector2 dir = Random.insideUnitCircle.normalized;

            float distance = Random.Range(2f, 3f);

            Vector3 spawnPos = center + (Vector3)(dir * distance);

            Instantiate(prefab, spawnPos, Quaternion.identity);
        }
    }

    public void SetQuestion(QuestionData question)
    {
        assignedQuestion = question;
    }
}
