
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MatchRevealUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private GameObject randomPickPanel;
    [SerializeField] private GameObject modePickPanel;
    [SerializeField] private GameObject roundRevealPanel;

    [Header("Mode Cards")]
    [SerializeField] private Transform modeCardContainer;
    [SerializeField] private MatchModeCardUI modeCardPrefab;

    [Header("Game Modes")]
    [SerializeField] private GameModeData[] gameModes;

    [Header("Round Reveal")]
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text modeText;
    [SerializeField] private TMP_Text locationText;

    [Header("Round Reveal Visuals")]
    [SerializeField] private Image modeBackgroundImage;
    [Header("Timing")]
    [SerializeField, Min(0f)] private float randomPickDuration = 0.7f;
    [SerializeField, Min(0f)] private float modePickDuration = 2f;
    [SerializeField, Min(0f)] private float roundRevealDuration = 1.5f;

    private readonly List<MatchModeCardUI> modeCards = new();

    private MatchSession matchSession;
    private Coroutine revealCoroutine;
    private int lastHandledSequence = -1;

    public event Action OnRevealCompleted;
private NetworkManager subscribedNetworkManager;
    public float TotalRevealDuration =>
        randomPickDuration + modePickDuration + roundRevealDuration;

    private void Awake()
    {
        if (rootPanel != null)
            rootPanel.SetActive(false);

        CreateModeCards();
    }
private void OnEnable()
{
    SubscribeToNetworkManager();
}

private void OnDisable()
{
    UnsubscribeFromNetworkManager();
    UnbindMatchSession();
    StopRevealCoroutine();

    if (rootPanel != null)
        rootPanel.SetActive(false);

    ResetCards();
    lastHandledSequence = -1;
}

    // ------------------------------------------------------------
    // SESSION BINDING
    // ------------------------------------------------------------

private void HandleMatchSessionRegistered(MatchSession session)
{
    if (session == null || !session.IsSpawned)
        return;

    if (matchSession == session)
        return;

    UnbindMatchSession();

    matchSession = session;
    matchSession.OnRoundRevealRequested += HandleRoundReveal;

    CheckForMissedReveal();
}
    private void BindMatchSession()
{

    MatchSession candidate = MatchSession.Instance;



    if (candidate == null || !candidate.IsSpawned)
        return;

    if (matchSession == candidate)
        return;

    UnbindMatchSession();

    matchSession = candidate;
    matchSession.OnRoundRevealRequested += HandleRoundReveal;


    CheckForMissedReveal();
}

    private void UnbindMatchSession()
    {
        if (matchSession == null)
            return;

        matchSession.OnRoundRevealRequested -= HandleRoundReveal;
        matchSession = null;
    }

    private void CheckForMissedReveal()
    {
        if (matchSession == null || !matchSession.IsSpawned)
            return;

        int sequence = matchSession.RoundRevealSequence;

        if (sequence <= 0 || sequence == lastHandledSequence)
            return;

        HandleRoundReveal();
    }

    // ------------------------------------------------------------
    // NETWORK SCENE EVENTS
    // ------------------------------------------------------------


private void SubscribeToNetworkManager()
{
    NetworkManager networkManager = NetworkManager.Instance;

    if (networkManager == null)
    {
        // Wait for NetworkManager to initialize.
        NetworkManager.OnInstanceReady -= HandleNetworkManagerReady;
        NetworkManager.OnInstanceReady += HandleNetworkManagerReady;


        return;
    }

    SubscribeToNetworkManagerEvents(networkManager);
}
private void HandleNetworkManagerReady(
    NetworkManager networkManager)
{
    NetworkManager.OnInstanceReady -= HandleNetworkManagerReady;

    if (!isActiveAndEnabled || networkManager == null)
        return;

    SubscribeToNetworkManagerEvents(networkManager);
}


private void SubscribeToNetworkManagerEvents(
    NetworkManager networkManager)
{
    if (subscribedNetworkManager == networkManager)
    {
        BindMatchSession();
        return;
    }

    UnsubscribeFromNetworkManager();

    subscribedNetworkManager = networkManager;

    subscribedNetworkManager.OnNetworkSceneLoadStartEvent
        += HideImmediately;

    subscribedNetworkManager.OnNetworkSceneLoadDoneEvent
        += HandleSceneLoadDone;

    subscribedNetworkManager.OnMatchSessionRegistered
        += HandleMatchSessionRegistered;


    // Handles a session that already exists.
    BindMatchSession();
}

private void UnsubscribeFromNetworkManager()
{
    // Remove the pending initialization subscription.
    NetworkManager.OnInstanceReady -= HandleNetworkManagerReady;

    // Unsubscribe from the exact manager we subscribed to.
    if (subscribedNetworkManager != null)
    {
        subscribedNetworkManager.OnNetworkSceneLoadStartEvent
            -= HideImmediately;

        subscribedNetworkManager.OnNetworkSceneLoadDoneEvent
            -= HandleSceneLoadDone;

        subscribedNetworkManager.OnMatchSessionRegistered
            -= HandleMatchSessionRegistered;

        subscribedNetworkManager = null;
    }
}
    private void HandleSceneLoadDone(Scene scene)
    {
        BindMatchSession();
        CheckForMissedReveal();
    }

    // ------------------------------------------------------------
    // MODE CARDS
    // ------------------------------------------------------------

    private void CreateModeCards()
    {
        ClearModeCards();

        if (modeCardPrefab == null || modeCardContainer == null)
        {

            return;
        }

        foreach (GameModeData data in gameModes)
        {
            if (data == null)
                continue;

            MatchModeCardUI card = Instantiate(
                modeCardPrefab,
                modeCardContainer
            );

            card.Setup(data);
            modeCards.Add(card);
        }
    }

    private void ClearModeCards()
    {
        foreach (MatchModeCardUI card in modeCards)
        {
            if (card != null)
                Destroy(card.gameObject);
        }

        modeCards.Clear();
    }

    private void ResetCards()
    {
        foreach (MatchModeCardUI card in modeCards)
        {
            if (card != null)
                card.ResetVisual();
        }
    }

    private void UpdateModeAvailability()
    {
        if (matchSession == null || !matchSession.IsSpawned)
            return;

        int usedMask = matchSession.UsedGameModeMask;
        int currentModeBit = 1 << (int)matchSession.CurrentGameMode;

        // Keep the mode selected for this round bright.
        int previouslyPlayedMask = usedMask & ~currentModeBit;

        foreach (MatchModeCardUI card in modeCards)
        {
            if (card == null || card.ModeData == null)
                continue;

            int modeBit = 1 << (int)card.ModeData.GameMode;
            bool alreadyPlayed = (previouslyPlayedMask & modeBit) != 0;

            card.SetAvailable(!alreadyPlayed);
        }
    }

    private List<GameModeData> GetAvailableModes()
    {
        List<GameModeData> availableModes = new();

        if (matchSession == null || !matchSession.IsSpawned)
            return availableModes;

        int usedMask = matchSession.UsedGameModeMask;

        foreach (GameModeData data in gameModes)
        {
            if (data == null)
                continue;

            int bit = 1 << (int)data.GameMode;

            if ((usedMask & bit) == 0)
                availableModes.Add(data);
        }

        // The server marks the selected mode as used before
        // the reveal animation finishes, so allow it in the roulette.
        GameModeData selectedMode =
            GetGameModeData(matchSession.CurrentGameMode);

        if (selectedMode != null &&
            !availableModes.Contains(selectedMode))
        {
            availableModes.Add(selectedMode);
        }

        return availableModes;
    }

    private GameModeData GetGameModeData(GameModeType gameMode)
    {
        foreach (GameModeData data in gameModes)
        {
            if (data != null && data.GameMode == gameMode)
                return data;
        }

        return null;
    }

    private void HighlightMode(GameModeData selectedMode)
    {
        foreach (MatchModeCardUI card in modeCards)
        {
            if (card == null || card.ModeData == null)
                continue;

            card.SetHighlighted(card.ModeData == selectedMode);
        }
    }

    // ------------------------------------------------------------
    // REVEAL SEQUENCE
    // ------------------------------------------------------------

    private void HandleRoundReveal()
    {
        if (!isActiveAndEnabled ||
            matchSession == null ||
            !matchSession.IsSpawned)
        {
            return;
        }

        int sequence = matchSession.RoundRevealSequence;

        if (sequence <= 0 || sequence == lastHandledSequence)
            return;

        lastHandledSequence = sequence;

        StopRevealCoroutine();

        AudioManager.Instance?.PlayMusic(MusicID.RoundReveal);
        revealCoroutine = StartCoroutine(PlayRevealSequence());
    }

    private IEnumerator PlayRevealSequence()
    {
        if (!IsSessionValid())
        {
            HideImmediately();
            yield break;
        }

        if (rootPanel != null)
            rootPanel.SetActive(true);

        ResetCards();
        UpdateModeAvailability();

        if (randomPickPanel != null)
            randomPickPanel.SetActive(true);

        if (modePickPanel != null)
            modePickPanel.SetActive(false);

        if (roundRevealPanel != null)
            roundRevealPanel.SetActive(false);

        yield return new WaitForSeconds(randomPickDuration);

        if (!IsSessionValid())
        {
            FinishRevealWithoutCompletion();
            yield break;
        }

        if (randomPickPanel != null)
            randomPickPanel.SetActive(false);

        if (modePickPanel != null)
            modePickPanel.SetActive(true);

        List<GameModeData> availableModes = GetAvailableModes();

        yield return StartCoroutine(
            AnimateModeSelection(availableModes)
        );

        if (!IsSessionValid())
        {
            FinishRevealWithoutCompletion();
            yield break;
        }

        if (modePickPanel != null)
            modePickPanel.SetActive(false);

        if (roundRevealPanel != null)
            roundRevealPanel.SetActive(true);

        GameModeData selectedMode =
            GetGameModeData(matchSession.CurrentGameMode);

        if (selectedMode != null)
        {
            if (modeText != null)
                modeText.text = selectedMode.DisplayName;

            if (locationText != null)
                locationText.text = selectedMode.LocationName;

            UpdateSelectedModeVisuals(selectedMode);
        }

        if (roundText != null)
            roundText.text = $"ROUND {matchSession.CurrentRound}";

        yield return new WaitForSeconds(roundRevealDuration);

        if (!IsSessionValid())
        {
            FinishRevealWithoutCompletion();
            yield break;
        }

        if (rootPanel != null)
            rootPanel.SetActive(false);


revealCoroutine = null;
OnRevealCompleted?.Invoke();
    }

    private IEnumerator AnimateModeSelection(
        List<GameModeData> availableModes)
    {
        if (!IsSessionValid() || availableModes.Count == 0)
            yield break;

        GameModeData selectedMode =
            GetGameModeData(matchSession.CurrentGameMode);

        int selectedIndex = availableModes.IndexOf(selectedMode);

        if (selectedIndex < 0)
            yield break;

        int startIndex = UnityEngine.Random.Range(
            0,
            availableModes.Count
        );

        int loops = availableModes.Count * 2;

        int totalSteps = loops +
            (selectedIndex - startIndex + availableModes.Count)
            % availableModes.Count;

        for (int i = 0; i <= totalSteps; i++)
        {
            if (!IsSessionValid())
                yield break;

            int index = (startIndex + i) % availableModes.Count;
            HighlightMode(availableModes[index]);

            float progress = (float)i / Mathf.Max(1, totalSteps);
            float delay = Mathf.Lerp(0.08f, 0.24f, progress);

            yield return new WaitForSeconds(delay);
        }

        HighlightMode(selectedMode);
        yield return new WaitForSeconds(0.4f);
        HighlightMode(null);
    }

    private void UpdateSelectedModeVisuals(GameModeData data)
    {
        if (data == null)
            return;

        if (modeBackgroundImage != null)
        {
            modeBackgroundImage.sprite = data.ModeBackground;
            modeBackgroundImage.enabled = data.ModeBackground != null;
        }
    }

    private bool IsSessionValid()
    {
        return matchSession != null && matchSession.IsSpawned;
    }

    private void FinishRevealWithoutCompletion()
    {
        if (rootPanel != null)
            rootPanel.SetActive(false);

        revealCoroutine = null;
    }

    private void StopRevealCoroutine()
    {
        if (revealCoroutine == null)
            return;

        StopCoroutine(revealCoroutine);
        revealCoroutine = null;
    }

    // ------------------------------------------------------------
    // PUBLIC METHODS
    // ------------------------------------------------------------

    public Sprite GetLoadingBackground(GameModeType gameMode)
    {
        GameModeData data = GetGameModeData(gameMode);

        return data != null ? data.LoadingBackground : null;
    }

    public void PlayCurrentRound()
    {
        BindMatchSession();

        if (!IsSessionValid() ||
            matchSession.RoundRevealSequence <= 0)
        {
            return;
        }

        // Explicitly requested replay, even if this sequence
        // has already been displayed.
        lastHandledSequence = -1;
        HandleRoundReveal();
    }

    public void HideImmediately()
    {
        StopRevealCoroutine();

        if (rootPanel != null)
            rootPanel.SetActive(false);

        ResetCards();
    }
}
