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
        if (!HasStateAuthority)
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
    }

    private void OnSmokeSpawned(
        NetworkRunner runner,
        NetworkObject obj)
    {
        SmokeCloud smoke = obj.GetComponent<SmokeCloud>();

        if (smoke != null)
            smoke.Initialize(Object.InputAuthority);

        Audio?.Play(SFXID.KapreSmoke);

        Debug.Log(
            $"[ABILITY] {GetType().Name} activated."
        );
    }
}