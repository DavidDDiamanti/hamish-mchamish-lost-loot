using UnityEngine;

// BillboardUI: keeps this transform's rotation matched to the main camera each LateUpdate.
// Used for world-space UI elements (e.g. health bars) that must always face the camera.
public class BillboardUI : MonoBehaviour
{
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void LateUpdate()
    {
        if (cam == null) return;

        transform.rotation = cam.transform.rotation;
    }
}