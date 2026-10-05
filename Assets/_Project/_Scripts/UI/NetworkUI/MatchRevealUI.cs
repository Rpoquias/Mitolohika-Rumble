using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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

    [Header("Timing")]
    [SerializeField] private float randomPickDuration = 0.7f;
    [SerializeField] private float modePickDuration = 2.0f;
    [SerializeField] private float roundRevealDuration = 1.5f;

    private readonly List<MatchModeCardUI> modeCards =
        new List<MatchModeCardUI>();

public event System.Action OnRevealCompleted;
    private MatchSession matchSession;
    private Coroutine revealCoroutine;

    private int lastHandledSequence = -1;

    public float TotalRevealDuration =>
        randomPickDuration +
        modePickDuration +
        roundRevealDuration;

    private void Start()
    {
        rootPanel.SetActive(false);

        CreateModeCards();

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance
                .OnNetworkSceneLoadStartEvent +=
                HideImmediately;
        }
    }

    private void Update()
    {
        if (matchSession == null)
        {
            matchSession = MatchSession.Instance;

            if (matchSession != null)
            {
                matchSession
                    .OnRoundRevealRequested +=
                    HandleRoundReveal;

                CheckForMissedReveal();
            }

            return;
        }

        if (matchSession.RoundRevealSequence !=
            lastHandledSequence)
        {
            CheckForMissedReveal();
        }
    }

    private void CreateModeCards()
    {
        ClearModeCards();

        if (modeCardPrefab == null)
        {
            Debug.LogError(
                "[MATCH REVEAL UI] " +
                "Mode Card Prefab is not assigned."
            );

            return;
        }

        if (modeCardContainer == null)
        {
            Debug.LogError(
                "[MATCH REVEAL UI] " +
                "Mode Card Container is not assigned."
            );

            return;
        }

        foreach (GameModeData modeData in gameModes)
        {
            if (modeData == null)
                continue;

            MatchModeCardUI card =
                Instantiate(
                    modeCardPrefab,
                    modeCardContainer
                );

            card.Setup(modeData);

            modeCards.Add(card);
        }

        Debug.Log(
            $"[MATCH REVEAL UI] Created " +
            $"{modeCards.Count} mode cards."
        );
    }

    private void ClearModeCards()
    {
        foreach (MatchModeCardUI card in modeCards)
        {
            if (card != null)
            {
                Destroy(card.gameObject);
            }
        }

        modeCards.Clear();
    }

    private void CheckForMissedReveal()
    {
        if (matchSession == null)
            return;

        if (matchSession.RoundRevealSequence ==
            lastHandledSequence)
        {
            return;
        }

        HandleRoundReveal();
    }

    private void HandleRoundReveal()
    {
        if (matchSession == null)
            return;

        if (matchSession.RoundRevealSequence <= 0)
            return;

        lastHandledSequence =
            matchSession.RoundRevealSequence;

        if (revealCoroutine != null)
        {
            StopCoroutine(
                revealCoroutine
            );
        }

        revealCoroutine =
            StartCoroutine(
                PlayRevealSequence()
            );
    }

    private IEnumerator PlayRevealSequence()
    {
        rootPanel.SetActive(true);

        ResetCards();

        // =========================
        // 1. RANDOM MATCH MODE PICK
        // =========================

        randomPickPanel.SetActive(true);
        modePickPanel.SetActive(false);
        roundRevealPanel.SetActive(false);

        yield return new WaitForSeconds(
            randomPickDuration
        );

        // =========================
        // 2. MODE SELECTION
        // =========================

        randomPickPanel.SetActive(false);
        modePickPanel.SetActive(true);

        List<GameModeData> availableModes =
            GetAvailableModes();

        yield return StartCoroutine(
            AnimateModeSelection(
                availableModes
            )
        );

        // =========================
        // 3. ROUND REVEAL
        // =========================

        modePickPanel.SetActive(false);
        roundRevealPanel.SetActive(true);

        GameModeData selectedMode =
            GetGameModeData(
                matchSession.CurrentGameMode
            );

        if (selectedMode != null)
        {
            modeText.text =
                selectedMode.DisplayName;

            locationText.text =
                selectedMode.LocationName;
        }

        roundText.text =
            $"ROUND {matchSession.CurrentRound}";

        yield return new WaitForSeconds(
            roundRevealDuration
        );
rootPanel.SetActive(false);

revealCoroutine = null;

OnRevealCompleted?.Invoke();
    }

    private IEnumerator AnimateModeSelection(
        List<GameModeData> availableModes)
    {
        if (availableModes.Count == 0)
        {
            Debug.LogError(
                "[MATCH REVEAL UI] " +
                "No available game modes."
            );

            yield break;
        }

        GameModeData selectedMode =
            GetGameModeData(
                matchSession.CurrentGameMode
            );

        if (selectedMode == null)
        {
            yield break;
        }

        int selectedIndex =
            availableModes.IndexOf(
                selectedMode
            );

        if (selectedIndex < 0)
        {
            Debug.LogError(
                "[MATCH REVEAL UI] " +
                "Selected mode is not in " +
                "the animation list."
            );

            yield break;
        }

        int startIndex =
            Random.Range(
                0,
                availableModes.Count
            );

        int loops =
            availableModes.Count * 2;

        int totalSteps =
            loops +
            (
                selectedIndex -
                startIndex +
                availableModes.Count
            ) % availableModes.Count;

        for (int i = 0;
             i <= totalSteps;
             i++)
        {
            int index =
                (startIndex + i) %
                availableModes.Count;

            HighlightMode(
                availableModes[index]
            );

            float progress =
                (float)i /
                Mathf.Max(
                    1,
                    totalSteps
                );

            float delay =
                Mathf.Lerp(
                    0.08f,
                    0.24f,
                    progress
                );

            yield return new WaitForSeconds(
                delay
            );
        }

        HighlightMode(
            selectedMode
        );

        yield return new WaitForSeconds(
            0.4f
        );

        HighlightMode(null);
    }

    private void HighlightMode(
        GameModeData selectedMode)
    {
        foreach (MatchModeCardUI card in modeCards)
        {
            if (card.ModeData == null)
                continue;

            card.SetHighlighted(
                card.ModeData == selectedMode
            );
        }
    }
public void PlayCurrentRound()
{
    if (matchSession == null)
    {
        matchSession = MatchSession.Instance;
    }

    if (matchSession == null)
    {
        Debug.LogError(
            "[MATCH REVEAL UI] MatchSession is missing."
        );

        return;
    }

    if (matchSession.RoundRevealSequence <= 0)
    {
        Debug.LogWarning(
            "[MATCH REVEAL UI] No round reveal available."
        );

        return;
    }

    HandleRoundReveal();
}
    private List<GameModeData>
        GetAvailableModes()
    {
        List<GameModeData> availableModes =
            new List<GameModeData>();

        int usedMask =
            matchSession.UsedGameModeMask;

        foreach (GameModeData modeData in gameModes)
        {
            if (modeData == null)
                continue;

            int bit =
                1 << (int)modeData.GameMode;

            bool available =
                (usedMask & bit) == 0;

            if (available)
            {
                availableModes.Add(
                    modeData
                );
            }
        }

        // The currently selected mode was
        // already marked as used by MatchSession.
        // Add it back so the roulette can land on it.
        GameModeData selectedMode =
            GetGameModeData(
                matchSession.CurrentGameMode
            );

        if (selectedMode != null &&
            !availableModes.Contains(selectedMode))
        {
            availableModes.Add(
                selectedMode
            );
        }

        return availableModes;
    }

    private GameModeData GetGameModeData(
        GameModeType gameMode)
    {
        foreach (GameModeData data in gameModes)
        {
            if (data == null)
                continue;

            if (data.GameMode == gameMode)
            {
                return data;
            }
        }

        Debug.LogError(
            $"[MATCH REVEAL UI] " +
            $"No GameModeData found for " +
            $"{gameMode}"
        );

        return null;
    }

    private void ResetCards()
    {
        foreach (MatchModeCardUI card in modeCards)
        {
            if (card != null)
            {
                card.ResetVisual();
            }
        }
    }

    public void HideImmediately()
    {
        if (revealCoroutine != null)
        {
            StopCoroutine(
                revealCoroutine
            );

            revealCoroutine = null;
        }

        rootPanel.SetActive(false);

        ResetCards();
    }

    private void OnDestroy()
    {
        if (matchSession != null)
        {
            matchSession
                .OnRoundRevealRequested -=
                HandleRoundReveal;
        }

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance
                .OnNetworkSceneLoadStartEvent -=
                HideImmediately;
        }
    }
}