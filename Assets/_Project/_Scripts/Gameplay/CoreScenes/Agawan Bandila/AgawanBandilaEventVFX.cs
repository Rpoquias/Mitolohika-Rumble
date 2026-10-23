
using System;
using Fusion;
using UnityEngine;

public class AgawanBandilaEventVFX : NetworkBehaviour
{
    [Header("Scene VFX")]
    [SerializeField] private GameObject speedSurgeVFX;
    [SerializeField] private GameObject slowMotionVFX;
    [SerializeField] private GameObject blackHoleVFX;

    // 0 = None, 1 = Speed Surge, 2 = Slow Motion, 3 = Black Hole.
    [Networked, OnChangedRender(nameof(ApplyVFX))]
    private int ActiveEventId { get; set; }

    public int CurrentEventId =>
        Object != null && Object.IsValid ? ActiveEventId : 0;

    public event Action<int> EventChanged;

    private int lastAppliedEventId = -1;

    public override void Spawned()
    {
        lastAppliedEventId = -1;
        ApplyVFX();
    }

    public void SetEvent(int eventId)
    {
        if (Object == null || !Object.HasStateAuthority)
            return;

        eventId = Mathf.Clamp(eventId, 0, 3);

        if (ActiveEventId == eventId)
            return;

        ActiveEventId = eventId;
        ApplyVFX();
    }

    private void ApplyVFX()
    {
        int id = ActiveEventId;

        SetActive(speedSurgeVFX, id == 1);
        SetActive(slowMotionVFX, id == 2);
        SetActive(blackHoleVFX, id == 3);

        // Notify listeners only when the event actually changes.
        if (lastAppliedEventId == id)
            return;

        lastAppliedEventId = id;
        EventChanged?.Invoke(id);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
            target.SetActive(active);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        SetActive(speedSurgeVFX, false);
        SetActive(slowMotionVFX, false);
        SetActive(blackHoleVFX, false);

        lastAppliedEventId = 0;
        EventChanged?.Invoke(0);
        EventChanged = null;
    }
}
