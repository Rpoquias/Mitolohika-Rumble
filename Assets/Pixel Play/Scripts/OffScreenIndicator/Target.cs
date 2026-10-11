using UnityEngine;

[DefaultExecutionOrder(0)]
public class Target : MonoBehaviour
{
    [Header("Indicator Settings")]
    [SerializeField] private Color targetColor = Color.red;
    [SerializeField] private bool needBoxIndicator = true;
    [SerializeField] private bool needArrowIndicator = true;
    [SerializeField] private bool needDistanceText = true;

    [HideInInspector]
    public Indicator indicator;

    public Color TargetColor => targetColor;
    public bool NeedBoxIndicator => needBoxIndicator;
    public bool NeedArrowIndicator => needArrowIndicator;
    public bool NeedDistanceText => needDistanceText;

    private bool isRegistered;

    private void OnEnable()
    {
        RegisterTarget();
    }

    private void OnDisable()
    {
        UnregisterTarget();
    }

    public void RegisterTarget()
    {
        if (isRegistered)
            return;

        isRegistered = true;

        OffScreenIndicator.TargetStateChanged?.Invoke(this, true);
    }

    public void UnregisterTarget()
    {
        if (!isRegistered)
            return;

        isRegistered = false;

        OffScreenIndicator.TargetStateChanged?.Invoke(this, false);
    }

    public float GetDistanceFromCamera(Vector3 cameraPosition)
    {
        return Vector3.Distance(cameraPosition, transform.position);
    }
}