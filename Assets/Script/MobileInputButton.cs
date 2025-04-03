using UnityEngine;
using UnityEngine.EventSystems;

public class MobileInputButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public string buttonName;
    private bool isPressed;
    private bool wasPressedThisFrame;

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        wasPressedThisFrame = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
    }

    public bool GetButton()
    {
        return isPressed;
    }

    public bool GetButtonDown()
    {
        if (wasPressedThisFrame)
        {
            wasPressedThisFrame = false;
            return true;
        }
        return false;
    }

    private void LateUpdate()
    {
        // Reset frame-based flags
        wasPressedThisFrame = false;
    }
}