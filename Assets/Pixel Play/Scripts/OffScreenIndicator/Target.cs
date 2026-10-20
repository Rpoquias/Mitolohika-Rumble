
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
    private bool isHidden;

    private void OnEnable()
    {
        UpdateRegistration();
    }

    private void OnDisable()
    {
        UnregisterTarget();
    }

    public void SetHidden(bool hidden)
    {
        if (isHidden == hidden)
            return;

        isHidden = hidden;
        UpdateRegistration();
    }

    private void UpdateRegistration()
    {
        if (!isActiveAndEnabled || isHidden)
        {
            UnregisterTarget();
            return;
        }

        RegisterTarget();
    }

    public void RegisterTarget()
    {
        if (isRegistered || !isActiveAndEnabled || isHidden)
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
