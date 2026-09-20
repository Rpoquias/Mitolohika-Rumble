using UnityEngine;
using UnityEngine.EventSystems;

public class MobileLookArea : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IDragHandler
{
    [SerializeField] private ThirdPersonCamera cameraController;

    private Vector2 lastPointerPosition;

    public void OnPointerDown(PointerEventData eventData)
    {
        lastPointerPosition = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 delta = eventData.position - lastPointerPosition;

        lastPointerPosition = eventData.position;

        cameraController.SetMobileLook(delta);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        cameraController.SetMobileLook(Vector2.zero);
    }
}