using Fusion;
using UnityEngine;

public class NunoProjectileAbility : CharacterAbility
{
    [Header("Projectile")]
    [SerializeField] private NetworkPrefabRef _projectilePrefab;
    [SerializeField] private Transform _projectileSpawnPoint;
    [SerializeField] private float _projectileSpeed = 12f;
    [SerializeField] private float _projectileLifetime = 3f;
    [SerializeField] private float _reversedInputDuration = 2f;

    public override void Activate()
    {
        if (!HasStateAuthority)
            return;

        if (!CanActivate())
            return;

        SpawnProjectile();
        StartCooldown();
    }

    private void SpawnProjectile()
    {
        Vector3 spawnPosition =
            _projectileSpawnPoint != null
                ? _projectileSpawnPoint.position
                : transform.position +
                  Vector3.up * 1f +
                  transform.forward * 0.8f;

        Quaternion spawnRotation =
            Quaternion.LookRotation(transform.forward);

        Runner.Spawn(
            _projectilePrefab,
            spawnPosition,
            spawnRotation,
            Object.InputAuthority,
            OnProjectileSpawned
        );
    }

    private void OnProjectileSpawned(
        NetworkRunner runner,
        NetworkObject networkObject)
    {
        NunoProjectile projectile =
            networkObject.GetComponent<NunoProjectile>();

        if (projectile == null)
        {
        

            runner.Despawn(networkObject);
            return;
        }

        projectile.SetOwner(Object.InputAuthority);

        projectile.Initialize(
            transform.forward,
            _projectileSpeed,
            _reversedInputDuration,
            _projectileLifetime
        );

        Audio?.Play(SFXID.NunoProjectile);
    }
}