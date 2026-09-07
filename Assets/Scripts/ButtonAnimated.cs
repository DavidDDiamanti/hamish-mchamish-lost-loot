using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ButtonAnimated: continuously animates a UI button with sine-wave bobbing, rotation, and optional
// scaling. A random timeOffset prevents multiple buttons from animating in sync.
public class ButtonAnimated : MonoBehaviour
{
    [Header("Bobbing")]
    [SerializeField] private float bobAmplitude = 2f;   // how high it moves (pixels)
    [SerializeField] private float bobSpeed = 2f;       // how fast it bobs

    [Header("Rotation")]
    [SerializeField] private float rotateAngle = 5f;    // max rotation angle (degrees)
    [SerializeField] private float rotateSpeed = 1f;    // how fast it rotates

    [Header("Scaling")]
    [SerializeField] private float scaleAmount = 0f;    // size change (0 = disabled)
    [SerializeField] private float scaleSpeed = 2f;     // speed of scaling

    private RectTransform rectTransform;
    private Vector3 startPos;
    private Vector3 startScale;

    private float timeOffset;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        startPos = rectTransform.anchoredPosition;
        startScale = rectTransform.localScale;

        // small random offset so multiple buttons don't move identically
        timeOffset = Random.Range(0f, 100f);
    }

    private void Update()
    {
        float t = Time.time + timeOffset;

        // Bobbing (vertical movement)
        float yOffset = Mathf.Sin(t * bobSpeed) * bobAmplitude;
        rectTransform.anchoredPosition = startPos + new Vector3(0f, yOffset, 0f);

        // Rotation
        float zRotation = Mathf.Sin(t * rotateSpeed) * rotateAngle;
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, zRotation);

        // Scaling
        float scaleOffset = (Mathf.Sin(t * scaleSpeed) + 1f) * (scaleAmount * 0.1f);
        rectTransform.localScale = startScale * (1f + scaleOffset);
    }
}