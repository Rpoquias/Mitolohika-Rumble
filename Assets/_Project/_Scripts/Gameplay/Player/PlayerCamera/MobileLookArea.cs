using UnityEngine;
using UnityEngine.EventSystems;

public class MobileLookArea : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IDragHandler
{

    [SerializeField] private ThirdPersonCamera cameraController;

    private int _activePointerId = -1;

    private void Start()
    {
        // Automatically find the ThirdPersonCamera in the scene if not assigned
        if (cameraController == null)
        {
            cameraController = FindAnyObjectByType<ThirdPersonCamera>();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Already being controlled by another finger.
        if (_activePointerId != -1)
            return;

        _activePointerId = eventData.pointerId;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Ignore every finger except the one controlling the camera.
        if (eventData.pointerId != _activePointerId)
            return;

        if (cameraController != null)
            cameraController.SetMobileLook(eventData.delta);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Only the active finger can release the camera.
        if (eventData.pointerId != _activePointerId)
            return;

        _activePointerId = -1;
        
        if (cameraController != null)
            cameraController.SetMobileLook(Vector2.zero);
    }

    private void OnDisable()
    {
        _activePointerId = -1;

        if (cameraController != null)
            cameraController.SetMobileLook(Vector2.zero);
    }
}