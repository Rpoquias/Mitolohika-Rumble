
using Fusion;
using UnityEngine;

public class AgawanBandilaEventVFX : NetworkBehaviour
{
    [Header("Scene VFX")]
    [SerializeField] private GameObject speedSurgeVFX;
    [SerializeField] private GameObject slowMotionVFX;
    [SerializeField] private GameObject blackHoleVFX;

    // 0 = None, 1 = Speed Surge, 2 = Slow Motion, 3 = Black Hole.
    [Networked] private int ActiveEventId { get; set; }

    private int _lastDisplayedEventId = -1;

    public override void Spawned()
    {
        _lastDisplayedEventId = -1;
        RefreshVFX();
    }

    private void Update()
    {
        RefreshVFX();
    }

    public void SetEvent(int eventId, float duration)
    {
        if (Object == null || !Object.HasStateAuthority)
            return;

        ActiveEventId = eventId;
    }

    private void RefreshVFX()
    {
        if (Object == null || !Object.IsValid)
            return;

        int eventId = ActiveEventId;

        if (eventId == _lastDisplayedEventId)
            return;

        _lastDisplayedEventId = eventId;

        SetVFXActive(speedSurgeVFX, eventId == 1);
        SetVFXActive(slowMotionVFX, eventId == 2);
        SetVFXActive(blackHoleVFX, eventId == 3);
    }

    private void SetVFXActive(GameObject vfx, bool active)
    {
        if (vfx != null && vfx.activeSelf != active)
            vfx.SetActive(active);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        SetVFXActive(speedSurgeVFX, false);
        SetVFXActive(slowMotionVFX, false);
        SetVFXActive(blackHoleVFX, false);
    }
}
