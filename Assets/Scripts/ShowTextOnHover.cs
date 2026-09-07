using UnityEngine;
using UnityEngine.EventSystems;

// ShowTextOnHover: shows a textBox GameObject when the pointer enters this UI element and hides
// it when the pointer exits. Used for tooltip display on shop items and other UI elements.
public class ShowTextOnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject textBox;

    private void Start()
    {
        if (textBox != null)
            textBox.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (textBox != null)
            textBox.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (textBox != null)
            textBox.SetActive(false);
    }
}