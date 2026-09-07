using UnityEngine;

// TopDownYSort: sets this renderer's sortingOrder each LateUpdate based on world Y position,
// so objects lower on screen render in front of objects higher up (top-down depth sorting).
// The offset field allows manual adjustment when the sprite pivot is not centered vertically.
[RequireComponent(typeof(SpriteRenderer))]
public class TopDownYSort : MonoBehaviour
{
    [SerializeField] private int offset = 0;
    [SerializeField] private bool runLateUpdate = true;

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        UpdateSorting();
    }

    private void LateUpdate()
    {
        if (runLateUpdate)
            UpdateSorting();
    }

    public void UpdateSorting()
    {
        sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f) + offset;
    }
}