using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class SpectatorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ThirdPersonCamera thirdPersonCamera;

    private NetworkRunner runner;
    private PlayerElimination localElimination;

    private readonly List<PlayerElimination> availablePlayers =
        new List<PlayerElimination>();

    private PlayerRef currentTargetPlayer;
    private bool hasTargetPlayer;
public event System.Action<bool> OnSpectatingChanged;
   public bool IsSpectating { get; private set; }
public bool HasTargetPlayer => hasTargetPlayer;
public PlayerRef CurrentTargetPlayer => currentTargetPlayer;
private bool spectatorResetLocked;

    private void Start()
    {
        if (thirdPersonCamera == null)
        {
            thirdPersonCamera =
                FindAnyObjectByType<ThirdPersonCamera>();
        }

        if (thirdPersonCamera == null)
        {
            Debug.LogError(
                "[SPECTATOR] ThirdPersonCamera not found."
            );

            enabled = false;
            return;
        }

        runner = thirdPersonCamera.Runner;
    }

  private void Update()
{
    if (runner == null || !runner.IsRunning)
    {
        if (thirdPersonCamera != null)
            runner = thirdPersonCamera.Runner;

        return;
    }

    if (!IsValidNetworkPlayer(localElimination))
    {
        localElimination = null;

        IsSpectating = false;
        hasTargetPlayer = false;
        currentTargetPlayer = default;

        availablePlayers.Clear();

        TryFindLocalPlayer();
        return;
    }

    if (spectatorResetLocked)
    {
        if (!localElimination.IsEliminated)
            spectatorResetLocked = false;

        return;
    }

    if (localElimination.IsEliminated)
    {
        if (!IsSpectating)
            StartSpectating();
    }
    else if (IsSpectating)
    {
        StopSpectating();
    }

    if (IsSpectating)
        ValidateCurrentTarget();
}
    private void TryFindLocalPlayer()
    {
        if (!runner.TryGetPlayerObject(
                runner.LocalPlayer,
                out NetworkObject playerObject))
        {
            return;
        }

        localElimination =
            playerObject.GetComponent<PlayerElimination>();

        if (localElimination == null)
        {
            Debug.LogWarning(
                "[SPECTATOR] Local player has no PlayerElimination."
            );
        }
    }
private bool IsValidNetworkPlayer(PlayerElimination player)
{
    if (player == null)
        return false;

    if (!player.isActiveAndEnabled)
        return false;

    if (!player.IsNetworkInitialized)
        return false;

    if (player.Object == null || !player.Object.IsValid)
        return false;

    return true;
}
   private void StartSpectating()
{
    IsSpectating = true;

    RefreshAvailablePlayers();

    if (availablePlayers.Count > 0)
    {
        SetTarget(0);
    }
    else
    {
        hasTargetPlayer = false;

        Debug.Log(
            "[SPECTATOR] No eligible players to spectate."
        );
    }

    OnSpectatingChanged?.Invoke(true);

    Debug.Log("[SPECTATOR] Spectating started.");
}

    public void NextPlayer()
    {
        if (!IsSpectating)
            return;

        RefreshAvailablePlayers();

        if (availablePlayers.Count == 0)
        {
            hasTargetPlayer = false;
            return;
        }

        int currentIndex = FindCurrentTargetIndex();

        int nextIndex = currentIndex < 0
            ? 0
            : (currentIndex + 1) % availablePlayers.Count;

        SetTarget(nextIndex);
    }

    public void PreviousPlayer()
    {
        if (!IsSpectating)
            return;

        RefreshAvailablePlayers();

        if (availablePlayers.Count == 0)
        {
            hasTargetPlayer = false;
            return;
        }

        int currentIndex = FindCurrentTargetIndex();

        int previousIndex = currentIndex < 0
            ? availablePlayers.Count - 1
            : (currentIndex - 1 + availablePlayers.Count)
              % availablePlayers.Count;

        SetTarget(previousIndex);
    }

    private void RefreshAvailablePlayers()
    {
        availablePlayers.Clear();

        if (runner == null || !runner.IsRunning)
            return;

        foreach (PlayerRef player in runner.ActivePlayers)
        {
            if (player == runner.LocalPlayer)
                continue;

            if (!runner.TryGetPlayerObject(
                    player,
                    out NetworkObject playerObject))
            {
                continue;
            }

            PlayerElimination elimination =
                playerObject.GetComponent<PlayerElimination>();

            if (elimination == null)
                continue;

            if (!IsEligibleTarget(elimination))
                continue;

            availablePlayers.Add(elimination);
        }

        availablePlayers.Sort(
            (a, b) => a.PlayerRef.RawEncoded.CompareTo(
                b.PlayerRef.RawEncoded
            )
        );
    }

private bool IsEligibleTarget(PlayerElimination player)
{
    return IsValidNetworkPlayer(player)
           && !player.IsEliminated;
}
    private void ValidateCurrentTarget()
    {
        if (!hasTargetPlayer)
        {
            RefreshAvailablePlayers();

            if (availablePlayers.Count > 0)
                SetTarget(0);

            return;
        }

        if (!runner.TryGetPlayerObject(
                currentTargetPlayer,
                out NetworkObject playerObject))
        {
            SelectReplacementTarget();
            return;
        }
PlayerElimination elimination =
    playerObject.GetComponent<PlayerElimination>();

if (!IsEligibleTarget(elimination))
{
    SelectReplacementTarget();
}
    }

    private void SelectReplacementTarget()
    {
        RefreshAvailablePlayers();

        if (availablePlayers.Count == 0)
        {
            hasTargetPlayer = false;
            return;
        }

        SetTarget(0);
    }

    private int FindCurrentTargetIndex()
    {
        for (int i = 0; i < availablePlayers.Count; i++)
        {
            if (availablePlayers[i].PlayerRef == currentTargetPlayer)
                return i;
        }

        return -1;
    }

    private void SetTarget(int index)
    {
        if (index < 0 || index >= availablePlayers.Count)
            return;

        PlayerElimination targetPlayer =
            availablePlayers[index];

        Transform cameraTarget =
            targetPlayer.transform.Find("CameraTarget");

        if (cameraTarget == null)
        {
            Debug.LogWarning(
                $"[SPECTATOR] CameraTarget missing on " +
                $"{targetPlayer.name}."
            );

            hasTargetPlayer = false;
            return;
        }

        currentTargetPlayer = targetPlayer.PlayerRef;
        hasTargetPlayer = true;

        thirdPersonCamera.SetTarget(cameraTarget);

        Debug.Log(
            $"[SPECTATOR] Now spectating {currentTargetPlayer}."
        );
    }

  private void StopSpectating()
{
    IsSpectating = false;
    hasTargetPlayer = false;
    currentTargetPlayer = default;

    availablePlayers.Clear();

    if (thirdPersonCamera != null)
        thirdPersonCamera.ReturnToLocalPlayer();

    OnSpectatingChanged?.Invoke(false);

    Debug.Log("[SPECTATOR] Spectating stopped.");
}

 public void ResetSpectator()
{
    localElimination = null;
    spectatorResetLocked = true;

    IsSpectating = false;
    hasTargetPlayer = false;
    currentTargetPlayer = default;

    availablePlayers.Clear();

    if (thirdPersonCamera != null)
        thirdPersonCamera.ReturnToLocalPlayer();

    OnSpectatingChanged?.Invoke(false);

    Debug.Log("[SPECTATOR] Spectator state reset.");
}
}