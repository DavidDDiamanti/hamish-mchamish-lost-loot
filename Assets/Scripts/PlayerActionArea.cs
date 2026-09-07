using System.Collections.Generic;
using UnityEngine;

// PlayerActionArea: the interaction trigger zone attached to the player.
// Tracks IInteractable objects entering/leaving its collider via physics 2D triggers.
// TryInteract() is called by Player on left mouse click and by the auto-click upgrade every frame.
// On each successful interact: plays ActionFlashEffect, triggers the attack animation, and calls
// Interact() on every IInteractable currently in range. Cooldown (clickRate) is shown via reloadBar.
// Dig speed upgrades reduce clickRate, increasing the fire rate.
public class PlayerActionArea : MonoBehaviour
{
    [SerializeField] private float clickRate = 0.5f;
    [SerializeField] private Transform reloadBar;

    [Header("Paw")]
    [SerializeField] private ActionFlashEffect[] actionFlash;
    [SerializeField] private Animator attackAnimator;

    private readonly List<IInteractable> interactablesInRange = new();
    private float lastClickTime = -Mathf.Infinity;

    private void Update()
    {
        float t = Mathf.Clamp01((Time.time - lastClickTime) / clickRate);

        if (reloadBar != null)
        {
            Vector3 s = reloadBar.localScale;
            s.x = t;
            reloadBar.localScale = s;
        }
    }

    public void increaseDigSpeed(float amount)
    {
        clickRate = Mathf.Max(0.1f, clickRate - amount);
    }

    public float GetClickRate() { return clickRate; }

    // Enforces the cooldown, fires animations, then calls Interact() on all targets in range.
    public void TryInteract(GameObject player)
    {
        if (lastClickTime + clickRate > Time.time) return;

        lastClickTime = Time.time;

        if (actionFlash != null)
        {
            foreach (var af in actionFlash)
                af.Play(clickRate);
        }

        if (attackAnimator != null)
        {
            attackAnimator.speed = 1f / clickRate;
            attackAnimator.ResetTrigger("Attack");
            attackAnimator.SetTrigger("Attack");
        }

        Debug.Log(clickRate);

        if (interactablesInRange.Count == 0) return;

        for (int i = interactablesInRange.Count - 1; i >= 0; i--)
        {
            IInteractable target = interactablesInRange[i];
            target.Interact(player);
        }

        if (reloadBar != null)
        {
            Vector3 s = reloadBar.localScale;
            s.x = 0f;
            reloadBar.localScale = s;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out IInteractable interactable))
        {
            if (!interactablesInRange.Contains(interactable))
                interactablesInRange.Add(interactable);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent(out IInteractable interactable))
            interactablesInRange.Remove(interactable);
    }

    public void ShowActionArea()
    {
        gameObject.SetActive(true);
    }

    public void HideActionArea()
    {
        gameObject.SetActive(false);
    }
}
