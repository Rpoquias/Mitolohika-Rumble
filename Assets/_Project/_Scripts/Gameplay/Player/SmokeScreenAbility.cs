using Fusion;
using UnityEngine;

public class SmokeScreenAbility : CharacterAbility
{
    [Header("Smoke Screen")]
    [SerializeField] private NetworkPrefabRef smokeCloudPrefab;

    private void Awake()
    {
        cooldown = 12f;
    }

    public override void Activate()
    {
        if (!Object.HasStateAuthority)
            return;

        if (!CanActivate())
        {
            Debug.Log(
                $"[ABILITY] {GetType().Name} is on cooldown."
            );

            return;
        }

        SpawnSmoke();

        StartCooldown();
    }

    private void SpawnSmoke()
    {
        Runner.Spawn(
        smokeCloudPrefab,
        transform.position,
        Quaternion.identity,
        Object.InputAuthority,
        OnSmokeSpawned
    );
        Debug.Log(
            $"[ABILITY] {GetType().Name} activated."
        );
    }
    private void OnSmokeSpawned(
    NetworkRunner runner,
    NetworkObject obj
)
{
    SmokeCloud smoke = obj.GetComponent<SmokeCloud>();

    smoke.Initialize(Object.InputAuthority);
}
}