using UnityEngine;

// CursorBehaviour: moves a custom cursor sprite to follow the mouse in world space.
// Hides the system cursor when hideSystemCursor is enabled.
public class CursorBehaviour : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] private Camera cam;
    [SerializeField] private bool hideSystemCursor = true;
    [SerializeField] private float zDepth = 0f;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (hideSystemCursor) Cursor.visible = false;

    }

    void Update()
    {

        FollowMouse();

    }

    void FollowMouse()
    {
        Vector3 mouse = Input.mousePosition;
        Vector3 world = cam.ScreenToWorldPoint(mouse);

        world.z = zDepth;

        transform.position = world;
    }

}
