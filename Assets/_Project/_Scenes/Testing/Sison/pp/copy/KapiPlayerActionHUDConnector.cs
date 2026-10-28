using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KapiPlayerActionHUDConnector : MonoBehaviour
{
    private Coroutine connectRoutine;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        StartConnecting();
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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => StartConnecting();

    private void StartConnecting()
    {
        if (connectRoutine != null)
            StopCoroutine(connectRoutine);
        connectRoutine = StartCoroutine(ConnectHUDRoutine());
    }

    private IEnumerator ConnectHUDRoutine()
    {
        const int maxAttempts = 120;
        for (int i = 0; i < maxAttempts; i++)
        {
            KapiPlayerActionHUD hud = FindAnyObjectByType<KapiPlayerActionHUD>();
            KapiPlayerMovement movement = GetComponent<KapiPlayerMovement>();
            KapiPlayerBumpAttack bump = GetComponent<KapiPlayerBumpAttack>();
            KapiCharacterAbility ability = GetComponent<KapiCharacterAbility>();
            if (hud != null && movement != null && bump != null && ability != null)
            {
                hud.SetPlayer(movement, bump, ability);
                connectRoutine = null;
                yield break;
            }
            yield return null;
        }
        connectRoutine = null;
        Debug.LogWarning("[PLAYER ACTION HUD] Could not connect to local player.");
    }
}