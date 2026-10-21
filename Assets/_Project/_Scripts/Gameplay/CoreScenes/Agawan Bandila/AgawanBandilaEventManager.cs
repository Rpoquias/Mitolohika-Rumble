using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class AgawanBandilaEventManager : MonoBehaviour
{
    private enum EventType
    {
        SpeedSurge, // VFX id 1
        SlowMotion, // VFX id 2
        BlackHole   // VFX id 3
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

    [Header("Late Spawns")]
    [Tooltip("While an event is active, how often to check for players whose " +
             "objects spawned after the event started.")]
    [SerializeField, Min(0.1f)]
    private float lateSpawnScanInterval = 0.5f;

    private bool _eventActive;
    private bool _hasPreviousEvent;
    private float _nextEventTime;
    private float _eventEndTime;
    private float _nextScanTime;
    private EventType _activeEventType;

    private readonly List<PlayerMovement> _players =
        new List<PlayerMovement>(4);

    // Players that already received the current event.
    private readonly HashSet<PlayerRef> _appliedPlayers =
        new HashSet<PlayerRef>();

    private NetworkRunner Runner =>
        NetworkManager.Instance != null
            ? NetworkManager.Instance.Runner
            : null;

    private void OnValidate()
    {
        // The next event must be able to start after the current one ends.
        eventInterval = Mathf.Max(eventInterval, eventDuration + 1f);
    }

    private void Awake()
    {
        // Update only needs to run while a round is active.
        enabled = false;
    }

    private bool HasServer()
    {
        NetworkRunner runner = Runner;

        return runner != null &&
               runner.IsRunning &&
               runner.IsServer;
    }

    private void Update()
    {
        if (!HasServer())
            return;

        float now = Time.time;

        if (_eventActive)
        {
            if (now >= _eventEndTime)
            {
                EndCurrentEvent();
                return;
            }

            // Catch players whose objects appeared after the event began.
            if (now >= _nextScanTime)
            {
                _nextScanTime = now + lateSpawnScanInterval;
                ApplyActiveEventToNewPlayers();
            }

            return;
        }

        if (now >= _nextEventTime)
            StartRandomEvent();
    }

    public void StartRoundEvents()
    {
        if (!HasServer())
            return;

        ResetAllEvents();
        SetVFX(0);

        _eventActive = false;
        _hasPreviousEvent = false;
        _nextEventTime = Time.time + eventInterval;

        enabled = true;
    }

    public void StopRoundEvents()
    {
        enabled = false;
        _eventActive = false;
        _appliedPlayers.Clear();

        if (!HasServer())
            return;

        ResetAllEvents();
        SetVFX(0);
    }

    private void StartRandomEvent()
    {
        EventType next = (EventType)Random.Range(0, 3);

        // Never repeat the previous event: shift to one of the other two.
        if (_hasPreviousEvent && next == _activeEventType)
            next = (EventType)(((int)next + Random.Range(1, 3)) % 3);

        _activeEventType = next;
        _hasPreviousEvent = true;

        if (next == EventType.BlackHole && blackHoleCenter == null)
        {
            Debug.LogWarning(
                "[BANDILA EVENTS] Black Hole center is not assigned.",
                this
            );
        }

        _appliedPlayers.Clear();
        ApplyActiveEventToNewPlayers();

        SetVFX((int)next + 1);

        _eventActive = true;
        _eventEndTime = Time.time + eventDuration;
        _nextEventTime = Time.time + eventInterval;
        _nextScanTime = Time.time + lateSpawnScanInterval;

    }

    private void EndCurrentEvent()
    {
        ResetAllEvents();
        SetVFX(0);
        _appliedPlayers.Clear();
        _eventActive = false;
    }

    private void SetVFX(int eventId)
    {
        if (eventVFX != null)
            eventVFX.SetEvent(eventId);
    }

    // ---------- Player effects ----------

    // Applies the active event to every player that has a spawned object
    // and hasn't received it yet. Safe to call repeatedly.
    private void ApplyActiveEventToNewPlayers()
    {
        NetworkRunner runner = Runner;

        if (runner == null)
            return;

        foreach (PlayerRef player in runner.ActivePlayers)
        {
            if (_appliedPlayers.Contains(player))
                continue;

            if (!runner.TryGetPlayerObject(player, out NetworkObject obj) ||
                obj == null)
            {
                continue; // Object not spawned yet; the next scan retries.
            }

            PlayerMovement movement =
                obj.GetComponentInChildren<PlayerMovement>();

            if (movement == null)
                continue;

            ApplyActiveEvent(movement);
            _appliedPlayers.Add(player);
        }
    }

    private void ApplyActiveEvent(PlayerMovement movement)
    {
        switch (_activeEventType)
        {
            case EventType.SpeedSurge:
                movement.SetGlobalEventSpeedMultiplier(speedSurgeMultiplier);
                break;

            case EventType.SlowMotion:
                movement.SetGlobalEventSpeedMultiplier(slowMotionMultiplier);
                break;

            case EventType.BlackHole:
                if (blackHoleCenter != null)
                {
                    movement.StartBlackHolePull(
                        blackHoleCenter.position,
                        blackHoleAcceleration,
                        blackHoleMaxSpeed
                    );
                }
                break;
        }
    }

    private List<PlayerMovement> CollectPlayers()
    {
        _players.Clear();

        NetworkRunner runner = Runner;

        if (runner == null)
            return _players;

        foreach (PlayerRef player in runner.ActivePlayers)
        {
            if (!runner.TryGetPlayerObject(player, out NetworkObject obj) ||
                obj == null)
            {
                continue;
            }

            PlayerMovement movement =
                obj.GetComponentInChildren<PlayerMovement>();

            if (movement != null)
                _players.Add(movement);
        }

        return _players;
    }

    // Resets everyone, including players who spawned after the last scan.
    private void ResetAllEvents()
    {
        List<PlayerMovement> players = CollectPlayers();

        for (int i = 0; i < players.Count; i++)
        {
            players[i].SetGlobalEventSpeedMultiplier(1f);
            players[i].ClearBlackHolePull();
        }
    }
}