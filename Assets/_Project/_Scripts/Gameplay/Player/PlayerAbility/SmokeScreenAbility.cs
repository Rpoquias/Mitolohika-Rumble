using Fusion;
using UnityEngine;

public class SmokeScreenAbility : CharacterAbility
{
    [Header("Smoke Screen")]
    [SerializeField] private NetworkPrefabRef smokeCloudPrefab;

    [SerializeField, Min(0f)]
    private float windupSeconds = 0.5f;

    [Networked]
    private TickTimer WindupTimer { get; set; }

    [Networked]
    private NetworkBool IsCasting { get; set; }

    private void Awake()
    {
        cooldown = 12f;
    }

    public override bool CanActivate()
    {
        return base.CanActivate() && !IsCasting;
    }

    public override void Activate()
    {
        if (!HasStateAuthority || !CanActivate())
            return;

        IsCasting = true;

        WindupTimer = TickTimer.CreateFromSeconds(
            Runner,
            windupSeconds
        );

        // Notify the presentation system that casting has started.
        NotifyActivated();

        // Start cooldown when casting begins.
        StartCooldown();

     
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (!HasStateAuthority || !IsCasting)
            return;

        if (!WindupTimer.Expired(Runner))
            return;

        IsCasting = false;
        WindupTimer = TickTimer.None;

        SpawnSmoke();
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

        
    }
}