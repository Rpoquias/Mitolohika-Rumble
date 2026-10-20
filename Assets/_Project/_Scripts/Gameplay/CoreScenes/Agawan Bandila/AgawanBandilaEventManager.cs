
using Fusion;
using UnityEngine;

public class AgawanBandilaEventManager : MonoBehaviour
{
    private enum EventType
    {
        SpeedSurge,
        SlowMotion,
        BlackHole
    }

    [Header("Timing")]
    [SerializeField, Min(1f)]
    private float eventInterval = 20f;

    [SerializeField, Min(1f)]
    private float eventDuration = 8f;

    [Header("Global Speed Events")]
    [SerializeField, Min(1f)]
    private float speedSurgeMultiplier = 1.3f;

    [SerializeField, Range(0.1f, 1f)]
    private float slowMotionMultiplier = 0.7f;

[Header("Black Hole")]
[SerializeField] private Transform blackHoleCenter;

[SerializeField, Min(0f)]
private float blackHoleAcceleration = 5f;

[SerializeField, Min(0f)]
private float blackHoleMaxSpeed = 2.5f;

[Header("Event Presentation")]
[SerializeField] private AgawanBandilaEventVFX eventVFX;
    private bool roundActive;
    private bool eventActive;

    private float nextEventTime;
    private float eventEndTime;

    private EventType activeEventType;

    private NetworkRunner Runner =>
        NetworkManager.Instance != null
            ? NetworkManager.Instance.Runner
            : null;

    private bool HasServer()
    {
        return Runner != null &&
               Runner.IsRunning &&
               Runner.IsServer;
    }

private void Update()
{
    if (!HasServer() || !roundActive)
        return;

    if (eventActive)
    {
        if (Time.time >= eventEndTime)
            EndCurrentEvent();

        return;
    }

    if (Time.time >= nextEventTime)
        StartRandomEvent();
}
private void StartBlackHoleForAllPlayers()
{
    if (blackHoleCenter == null)
    {
        Debug.LogWarning(
            "[BANDILA EVENTS] Black Hole center is not assigned.",
            this
        );

        return;
    }

    NetworkRunner runner = Runner;

    if (runner == null)
        return;

    foreach (PlayerRef player in runner.ActivePlayers)
    {
        NetworkObject playerObject = runner.GetPlayerObject(player);

        if (playerObject == null)
            continue;

        PlayerMovement movement =
            playerObject.GetComponentInChildren<PlayerMovement>();

        if (movement == null)
            continue;

        movement.StartBlackHolePull(
            blackHoleCenter.position,
            blackHoleAcceleration,
            blackHoleMaxSpeed
        );
        Debug.Log(
    $"[BLACK HOLE] Activated for {playerObject.name}. " +
    $"Center: {blackHoleCenter.position}"
);
    }
}
  public void StartRoundEvents()
{
    if (!HasServer())
        return;

    ResetAllEvents();

    eventVFX?.SetEvent(0, 0f);

    roundActive = true;
    eventActive = false;
    eventHasPlayed = false;

    nextEventTime = Time.time + eventInterval;
}
 


public void StopRoundEvents()
{
    if (!HasServer())
        return;

    roundActive = false;
    eventActive = false;

    ResetAllEvents();

    eventVFX?.SetEvent(0, 0f);
}
private void StartRandomEvent()
{
    if (!HasServer() || eventActive)
        return;

    EventType newEvent;

    // Avoid repeating the immediately previous event.
    do
    {
        newEvent = (EventType)Random.Range(0, 3);
    }
    while (newEvent == activeEventType && eventHasPlayed);

    activeEventType = newEvent;
    eventHasPlayed = true;

 switch (activeEventType)
{
    case EventType.SpeedSurge:
        SetAllPlayerSpeeds(speedSurgeMultiplier);
        break;

    case EventType.SlowMotion:
        SetAllPlayerSpeeds(slowMotionMultiplier);
        break;

    case EventType.BlackHole:
        StartBlackHoleForAllPlayers();
        break;
}int eventId = activeEventType switch
{
    EventType.SpeedSurge => 1,
    EventType.SlowMotion => 2,
    EventType.BlackHole => 3,
    _ => 0
};

eventVFX?.SetEvent(eventId, eventDuration);

eventActive = true;
eventEndTime = Time.time + eventDuration;
nextEventTime = Time.time + eventInterval;

Debug.Log(
    $"[BANDILA EVENTS] Started {activeEventType} " +
    $"for {eventDuration:0.#} seconds."
);
}


    private bool eventHasPlayed;

private void EndCurrentEvent()
{
    if (!HasServer())
        return;

    ResetAllEvents();

    eventVFX?.SetEvent(0, 0f);

    eventActive = false;
}    private void SetAllPlayerSpeeds(float multiplier)
    {
        NetworkRunner runner = Runner;

        if (runner == null)
            return;

        foreach (PlayerRef player in runner.ActivePlayers)
        {
            NetworkObject playerObject =
                runner.GetPlayerObject(player);

            if (playerObject == null)
                continue;

            PlayerMovement movement =
                playerObject.GetComponentInChildren<PlayerMovement>();

            if (movement != null)
                movement.SetGlobalEventSpeedMultiplier(multiplier);
        }
    }


private void ResetAllEvents()
{
    SetAllPlayerSpeeds(1f);
    ClearBlackHoleFromAllPlayers();
}

private void ClearBlackHoleFromAllPlayers()
{
    NetworkRunner runner = Runner;

    if (runner == null)
        return;

    foreach (PlayerRef player in runner.ActivePlayers)
    {
        NetworkObject playerObject = runner.GetPlayerObject(player);

        if (playerObject == null)
            continue;

        PlayerMovement movement =
            playerObject.GetComponentInChildren<PlayerMovement>();

        if (movement != null)
            movement.ClearBlackHolePull();
    }
}

}
