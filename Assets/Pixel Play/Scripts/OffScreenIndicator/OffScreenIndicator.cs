using PixelPlay.OffScreenIndicator;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Fusion;
using System.Collections;
/// <summary>
/// Attach the script to the off screen indicator panel.
/// </summary>
[DefaultExecutionOrder(-1)]
public class OffScreenIndicator : MonoBehaviour
{
    [Range(0.5f, 0.9f)]
    [Tooltip("Distance offset of the indicators from the centre of the screen")]
    [SerializeField] private float screenBoundOffset = 0.9f;

    private Camera mainCamera;
    private Vector3 screenCentre;
    private Vector3 screenBounds;

    private List<Target> targets = new List<Target>();

    public static Action<Target, bool> TargetStateChanged;

  private void Awake()
{
    mainCamera = Camera.main;
    screenCentre = new Vector3(Screen.width, Screen.height, 0) / 2;
    screenBounds = screenCentre * screenBoundOffset;

    TargetStateChanged += HandleTargetStateChanged;
}

    void LateUpdate()
    {
        DrawIndicators();
    }

    /// <summary>
    /// Draw the indicators on the screen and set thier position and rotation and other properties.
    /// </summary>
    /// 
    /// 
   private void DrawIndicators()
{
    foreach (Target target in targets)
    {
        if (target == null)
            continue;

        // Never show an indicator for the locally controlled player.
        if (IsLocalPlayerTarget(target))
        {
            target.indicator?.Activate(false);
            target.indicator = null;
            continue;
        }

        Vector3 screenPosition =
            OffScreenIndicatorCore.GetScreenPosition(
                mainCamera,
                target.transform.position
            );

        bool isTargetVisible =
            OffScreenIndicatorCore.IsTargetVisible(screenPosition);

        float distanceFromCamera = target.NeedDistanceText
            ? target.GetDistanceFromCamera(mainCamera.transform.position)
            : float.MinValue;

        Indicator indicator = null;

        if (isTargetVisible)
        {
            if (target.NeedBoxIndicator)
            {
                screenPosition.z = 0f;

                indicator = GetIndicator(
                    ref target.indicator,
                    IndicatorType.BOX
                );
            }
            else
            {
                // The target is visible, so its off-screen arrow is unnecessary.
                target.indicator?.Activate(false);
                target.indicator = null;
            }
        }
        else
        {
            if (target.NeedArrowIndicator)
            {
                float angle = 0f;

                OffScreenIndicatorCore.GetArrowIndicatorPositionAndAngle(
                    ref screenPosition,
                    ref angle,
                    screenCentre,
                    screenBounds
                );

                indicator = GetIndicator(
                    ref target.indicator,
                    IndicatorType.ARROW
                );

                indicator.transform.rotation =
                    Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg);
            }
            else
            {
                // No indicator is needed for this target.
                target.indicator?.Activate(false);
                target.indicator = null;
            }
        }

        if (indicator != null)
        {
            indicator.SetImageColor(target.TargetColor);
            indicator.SetDistanceText(distanceFromCamera);
            indicator.transform.position = screenPosition;
            indicator.SetTextRotation(Quaternion.identity);
        }
    }
}
    private bool IsLocalPlayerTarget(Target target)
{
    if (target == null)
        return false;

    NetworkObject networkObject =
        target.GetComponentInParent<NetworkObject>();

    // Non-networked targets, such as objectives, should still show indicators.
    if (networkObject == null)
        return false;

    // Wait until Fusion initializes the network object.
    if (!networkObject.IsValid)
        return true;

    // Hide the indicator only for the locally controlled player.
    return networkObject.HasInputAuthority;
}
private IEnumerator Start()
{
    // Let the scene and network objects initialize first.
    yield return null;

    RefreshTargets();

    // Allow Fusion another frame to finish assigning authority.
    yield return null;

    RefreshTargets();
}
    /// <summary>
    /// 1. Add the target to targets list if <paramref name="active"/> is true.
    /// 2. If <paramref name="active"/> is false deactivate the targets indicator, 
    ///     set its reference null and remove it from the targets list.
    /// </summary>
    /// <param name="target"></param>
    /// <param name="active"></param>
private void HandleTargetStateChanged(Target target, bool active)
{
    if (target == null)
        return;

    if (active)
    {
        if (!targets.Contains(target))
            targets.Add(target);
    }
    else
    {
        target.indicator?.Activate(false);
        target.indicator = null;

        targets.Remove(target);
    }
}
private void RefreshTargets()
{
    targets.RemoveAll(target => target == null);

    Target[] sceneTargets = FindObjectsByType<Target>();

    foreach (Target target in sceneTargets)
    {
        if (!target.isActiveAndEnabled)
            continue;

        if (!targets.Contains(target))
            targets.Add(target);
    }
}

    /// <summary>
    /// Get the indicator for the target.
    /// 1. If its not null and of the same required <paramref name="type"/> 
    ///     then return the same indicator;
    /// 2. If its not null but is of different type from <paramref name="type"/> 
    ///     then deactivate the old reference so that it returns to the pool 
    ///     and request one of another type from pool.
    /// 3. If its null then request one from the pool of <paramref name="type"/>.
    /// </summary>
    /// <param name="indicator"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    private Indicator GetIndicator(ref Indicator indicator, IndicatorType type)
    {
        if(indicator != null)
        {
            if(indicator.Type != type)
            {
                indicator.Activate(false);
                indicator = type == IndicatorType.BOX ? BoxObjectPool.current.GetPooledObject() : ArrowObjectPool.current.GetPooledObject();
                indicator.Activate(true); // Sets the indicator as active.
            }
        }
        else
        {
            indicator = type == IndicatorType.BOX ? BoxObjectPool.current.GetPooledObject() : ArrowObjectPool.current.GetPooledObject();
            indicator.Activate(true); // Sets the indicator as active.
        }
        return indicator;
    }

  private void OnDestroy()
{
    TargetStateChanged -= HandleTargetStateChanged;
}
}
