using UnityEngine;

// MouseOrbit: positions and rotates a child object to orbit the player pointing toward the mouse.
// Used as an aim indicator; snaps to the last known direction when the mouse is stationary.
public class MouseOrbit : MonoBehaviour
{
    public Transform player;
    public float radius;

    private Vector3 lastDir = Vector3.right;

    void Awake()
    {
        radius = transform.lossyScale.x;
    }

    void Update()
    {

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = player.position.z;

        Vector3 dir = mouseWorld - player.position;

        if (dir.sqrMagnitude > 0.0001f)
            lastDir = dir.normalized;

        transform.position = player.position + lastDir * radius;

        float angle = Mathf.Atan2(lastDir.y, lastDir.x) * Mathf.Rad2Deg + 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
}
