
using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerActionHUDConnector : NetworkBehaviour
{
    private Coroutine connectRoutine;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (connectRoutine != null)
        {
            StopCoroutine(connectRoutine);
            connectRoutine = null;
        }
    }

    public override void Spawned()
    {
        if (!HasInputAuthority)
            return;

        StartConnecting();
    }

    private void OnSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        if (Object == null || !HasInputAuthority)
            return;

        StartConnecting();
    }

    private void StartConnecting()
    {
        if (connectRoutine != null)
            StopCoroutine(connectRoutine);

        connectRoutine = StartCoroutine(ConnectHUDRoutine());
    }

    private IEnumerator ConnectHUDRoutine()
    {
        // Retry for up to roughly 2 seconds at 60 FPS.
        const int maxAttempts = 120;

        for (int i = 0; i < maxAttempts; i++)
        {
            if (!HasInputAuthority || Object == null)
                break;

            PlayerActionHUD hud =
                FindAnyObjectByType<PlayerActionHUD>();

            if (hud != null)
            {
                PlayerMovement movement =
                    GetComponent<PlayerMovement>();

                PlayerBumpAttack bump =
                    GetComponent<PlayerBumpAttack>();

                CharacterAbility ability =
                    GetComponent<CharacterAbility>();

                if (movement != null &&
                    bump != null &&
                    ability != null)
                {
                    hud.SetPlayer(movement, bump, ability);

                    Debug.Log(
                        "[PLAYER ACTION HUD] Connected to local player."
                    );

                    connectRoutine = null;
                    yield break;
                }
            }

            yield return null;
        }

        connectRoutine = null;

        Debug.LogWarning(
            "[PLAYER ACTION HUD] Could not connect. " +
            "HUD or player components were unavailable."
        );
    }

    
public override void Despawned(NetworkRunner runner, bool hasState)
{
    if (!HasInputAuthority)
        return;

    PlayerActionHUD hud =
        FindAnyObjectByType<PlayerActionHUD>();

    if (hud != null)
        hud.ClearPlayer();
}

}
