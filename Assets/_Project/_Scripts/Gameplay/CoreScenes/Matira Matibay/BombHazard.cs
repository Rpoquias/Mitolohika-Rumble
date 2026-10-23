
using Fusion;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(NetworkObject))]
public class BombHazard : NetworkBehaviour
{
    [Header("Explosion")]
    [SerializeField] private float warningDuration = 3f;
    [SerializeField] private float explosionRadius = 4f;
    [SerializeField] private float horizontalKnockback = 9f;
    [SerializeField] private float upwardKnockback = 5f;
    [SerializeField] private float hitstopDuration = 0.1f;
    [SerializeField] private float explosionVisualDuration = 0.6f;

[Header("Explosion Visual")]
[SerializeField] private GameObject explosionEffectPrefab;
[SerializeField] private Vector3 explosionEffectOffset = Vector3.zero;

[Header("Countdown")]
[SerializeField] private TMP_Text countdownText;

    [Header("Audio")]
    [SerializeField] private SFXID explosionSFX = SFXID.ArenaTileDestroyed;

    [Networked] private TickTimer ExplosionTimer { get; set; }
    [Networked] private TickTimer DespawnTimer { get; set; }
    [Networked] private NetworkBool HasExploded { get; set; }

    private MatiraMatibayHazardManager ownerManager;
private int lastCountdownValue = -1;

    public void SetOwnerManager(MatiraMatibayHazardManager manager)
    {
        ownerManager = manager;
    }

public override void Spawned()
{
    lastCountdownValue = -1;

    if (HasStateAuthority)
    {
        ExplosionTimer = TickTimer.CreateFromSeconds(
            Runner,
            Mathf.Max(0f, warningDuration));
    }

    if (countdownText != null)
        countdownText.gameObject.SetActive(true);
}

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (HasExploded)
        {
            if (DespawnTimer.Expired(Runner))
                Runner.Despawn(Object);

            return;
        }

        if (ExplosionTimer.Expired(Runner))
            Explode();
    }

public override void Render()
{
    if (HasExploded)
    {
        if (countdownText != null &&
            countdownText.gameObject.activeSelf)
        {
            countdownText.gameObject.SetActive(false);
        }

        return;
    }

    UpdateCountdownFacing();

    if (countdownText == null || Runner == null)
        return;

    float remaining =
        ExplosionTimer.RemainingTime(Runner) ?? 0f;

    int countdown = Mathf.CeilToInt(remaining);

    if (countdown == lastCountdownValue)
        return;

    lastCountdownValue = countdown;
    countdownText.text = countdown.ToString();
}
    private void Explode()
    {
        if (!HasStateAuthority || HasExploded)
            return;
HasExploded = true;

if (countdownText != null)
    countdownText.gameObject.SetActive(false);

RPC_PlayExplosionEffect(transform.position);

ApplyExplosionKnockback();
RPC_PlayExplosionSFX(transform.position);

DespawnTimer = TickTimer.CreateFromSeconds(
    Runner,
    Mathf.Max(0.05f, explosionVisualDuration));
    }


[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
private void RPC_PlayExplosionEffect(Vector3 position)
{
    if (explosionEffectPrefab == null)
        return;

    GameObject effect = Instantiate(
        explosionEffectPrefab,
        position + explosionEffectOffset,
        Quaternion.identity
    );

    Destroy(effect, Mathf.Max(0.1f, explosionVisualDuration));
}

private void UpdateCountdownFacing()
{
    if (countdownText == null)
        return;

    if (Runner == null ||
        !ThirdPersonCamera.TryGetCamera(
            Runner,
            out ThirdPersonCamera camera) ||
        camera == null)
    {
        return;
    }

    Transform cameraTransform = camera.CameraTransform;

    // Face the camera while keeping the text upright.
    Vector3 direction =
        countdownText.transform.position -
        cameraTransform.position;

    direction.y = 0f;

    if (direction.sqrMagnitude < 0.001f)
        return;

    countdownText.transform.rotation =
        Quaternion.LookRotation(direction);
}

    private void ApplyExplosionKnockback()
    {
        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            if (!Runner.TryGetPlayerObject(player, out NetworkObject playerObject))
                continue;

            PlayerMovement movement =
                playerObject.GetComponent<PlayerMovement>();

            if (movement == null)
                continue;

            Vector3 offset =
                playerObject.transform.position - transform.position;

            // Check horizontal distance so the bomb's height
            // doesn't unexpectedly reduce its effective radius.
            Vector2 flatOffset = new Vector2(offset.x, offset.z);

            if (flatOffset.sqrMagnitude > explosionRadius * explosionRadius)
                continue;

            Vector3 direction = flatOffset.sqrMagnitude > 0.001f
                ? new Vector3(flatOffset.x, 0f, flatOffset.y).normalized
                : Vector3.forward;

            Vector3 knockback = direction * horizontalKnockback;
            knockback.y = upwardKnockback;

            movement.ApplyKnockback(
                knockback,
                hitstopDuration,
                HitType.PowerfulKick
            );
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayExplosionSFX(Vector3 position)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFXAtPosition(
                explosionSFX, position);
        }
    }

public override void Despawned(
    NetworkRunner runner,
    bool hasState)
{
    if (ownerManager != null)
        ownerManager.NotifyBombDespawned(Object);

    ownerManager = null;
}
    #if UNITY_EDITOR
private void OnDrawGizmosSelected()
{
    Gizmos.color = Color.red;

    // Match the explosion's horizontal radius check.
    Vector3 center = transform.position;
    center.y = transform.position.y;

    Gizmos.DrawWireSphere(center, explosionRadius);

    // Draw the effective horizontal blast radius.
    Gizmos.color = Color.yellow;
    Vector3 right = Vector3.right * explosionRadius;
    Vector3 forward = Vector3.forward * explosionRadius;

    Gizmos.DrawLine(center - right, center + right);
    Gizmos.DrawLine(center - forward, center + forward);
}
#endif
}
