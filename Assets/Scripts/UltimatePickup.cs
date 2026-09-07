using UnityEngine;

// UltimatePickup: ILoot that applies all temporary stat boosts at once via Player.ApplyUltimatePickup().
// Displays a rainbow cycling colour on its SpriteRenderer using a random per-instance hue offset
// so multiple pickups don't pulse in sync.
public class UltimatePickup : MonoBehaviour, ILoot
{
    [Header("Rainbow Settings")]
    [SerializeField] private float cycleSpeed = 1f;
    [SerializeField] private float saturation = 1f;
    [SerializeField] private float brightness = 1f;
    [SerializeField] private bool useUnscaledTime = false;

    private SpriteRenderer sr;
    private float hueOffset;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        hueOffset = Random.value;
    }

    private void Update()
    {
        float t = useUnscaledTime ? Time.unscaledTime : Time.time;
        float hue = Mathf.Repeat(hueOffset + t * cycleSpeed, 1f);

        Color rainbow = Color.HSVToRGB(hue, saturation, brightness);
        sr.color = rainbow;
    }

    public void Collect(Player player)
    {
        player.ApplyUltimatePickup();
    }
}
