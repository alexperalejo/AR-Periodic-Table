using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class ElementTileSelector : MonoBehaviour
{
    public ElementLoader elementLoader;
    public ElementInfoCard infoCard;
    public Camera arCamera;

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    void Update()
    {
        // Handle touch input (Android)
        if (Touch.activeTouches.Count > 0)
        {
            var touch = Touch.activeTouches[0];
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                TrySelectTile(touch.screenPosition);
            }
        }

        // Handle mouse input (Editor testing)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TrySelectTile(Mouse.current.position.ReadValue());
        }
    }

    void TrySelectTile(Vector2 screenPosition)
    {
        if (arCamera == null) { Debug.LogError("AR Camera is null!"); return; }

        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Debug.Log("Hit: " + hit.collider.gameObject.name);
            ElementTile tile = hit.collider.GetComponent<ElementTile>();
            if (tile != null)
            {
                Debug.Log("Element tapped: " + tile.atomicNumber);
                elementLoader.LoadElement(tile.atomicNumber);
            }
        }
        else
        {
            Debug.Log("Raycast hit nothing");
        }
    }
}