
using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;

public class MatchResultUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject rootPanel;

    [Header("Round Information")]
    [SerializeField] private GameObject roundInfoObject;
    [SerializeField] private TMP_Text roundText;

    [Header("Results")]
    [SerializeField] private Transform resultContainer;
    [SerializeField] private MatchResultCardUI resultCardPrefab;

    [Header("Character Data")]
    [SerializeField] private CharacterData[] characterDatabase;

    [Header("Timing")]
    [SerializeField, Min(0f)]
    private float resultDisplayDuration = 3f;

    private MatchSession matchSession;
    private Coroutine resultCoroutine;
    private Coroutine bindCoroutine;

    private int lastHandledSequence = -1;

    private GameplayCanvasVisibility gameplayCanvasVisibility;
    private bool gameplayCanvasesHidden;

    private readonly List<MatchResultCardUI> resultCards = new();

    public event Action OnResultCompleted;

    private void Awake()
    {
        if (rootPanel != null)
            rootPanel.SetActive(false);

        if (roundInfoObject != null)
            roundInfoObject.SetActive(true);
    }

    private void OnEnable()
    {
        if (bindCoroutine != null)
            StopCoroutine(bindCoroutine);

        bindCoroutine = StartCoroutine(BindMatchSessionWhenAvailable());
    }

    private void OnDisable()
    {
        if (bindCoroutine != null)
        {
            StopCoroutine(bindCoroutine);
            bindCoroutine = null;
        }

        UnbindMatchSession();
        StopResultCoroutine();
        ClearCards();

        if (rootPanel != null)
            rootPanel.SetActive(false);

        RestoreGameplayCanvases();
        lastHandledSequence = -1;
    }

    private IEnumerator BindMatchSessionWhenAvailable()
    {
        while (isActiveAndEnabled)
        {
            MatchSession candidate = MatchSession.Instance;

            if (candidate != null && candidate.IsSpawned)
            {
                if (matchSession != candidate)
                {
                    UnbindMatchSession();

                    matchSession = candidate;
                    matchSession.OnRoundResultRequested += HandleRoundResult;

                    Debug.Log("[MATCH RESULT UI] Bound to MatchSession.");
                }

                CheckForMissedResult();

                bindCoroutine = null;
                yield break;
            }

            yield return null;
        }

        bindCoroutine = null;
    }

    private void UnbindMatchSession()
    {
        if (matchSession == null)
            return;

        matchSession.OnRoundResultRequested -= HandleRoundResult;
        matchSession = null;
    }

    private void CheckForMissedResult()
    {
        if (matchSession == null || !matchSession.IsSpawned)
            return;

        int sequence = matchSession.RoundResultSequence;

        if (sequence <= 0 || sequence == lastHandledSequence)
            return;

        HandleRoundResult();
    }

    private void HandleRoundResult()
    {
        if (!isActiveAndEnabled ||
            matchSession == null ||
            !matchSession.IsSpawned)
        {
            return;
        }

        int sequence = matchSession.RoundResultSequence;

        if (sequence <= 0 || sequence == lastHandledSequence)
            return;

        lastHandledSequence = sequence;

        StopResultCoroutine();
        resultCoroutine = StartCoroutine(ShowResultCoroutine());
    }

    private IEnumerator ShowResultCoroutine()
    {
        HideGameplayCanvases();

        if (rootPanel != null)
            rootPanel.SetActive(true);

        ClearCards();

        if (roundInfoObject != null)
            roundInfoObject.SetActive(true);

        if (roundText != null)
            roundText.text = $"ROUND {matchSession.CurrentRound} RESULTS";

        List<MatchSession.RoundResultRecord> results = new();

        for (int i = 0; i < matchSession.CurrentRoundResultCount; i++)
        {
            if (matchSession.TryGetCurrentRoundResult(
                    i,
                    out MatchSession.RoundResultRecord result))
            {
                results.Add(result);
            }
        }

        results.Sort((a, b) => a.placement.CompareTo(b.placement));

        if (resultCardPrefab != null && resultContainer != null)
        {
            foreach (MatchSession.RoundResultRecord result in results)
            {
                MatchResultCardUI card = Instantiate(
                    resultCardPrefab,
                    resultContainer
                );

                CharacterData characterData =
                    GetCharacterDataForPlayer(result.player);

                card.Setup(
                    result.placement,
                    result.player,
                    GetResultDetail(result),
                    characterData
                );

                resultCards.Add(card);
            }
        }
        else
        {
            Debug.LogWarning(
                "[MATCH RESULT UI] Result card prefab or container is missing."
            );
        }

        yield return new WaitForSeconds(resultDisplayDuration);

        resultCoroutine = null;

        HideImmediately();
        OnResultCompleted?.Invoke();
    }

    private CharacterData GetCharacterDataForPlayer(PlayerRef player)
    {
        NetworkManager networkManager = NetworkManager.Instance;

        if (networkManager == null || characterDatabase == null)
            return null;

        if (!networkManager.TryGetPlayerSelection(
                player,
                out CharacterID selectedCharacter))
        {
            return null;
        }

        for (int i = 0; i < characterDatabase.Length; i++)
        {
            CharacterData data = characterDatabase[i];

            if (data != null && data.characterID == selectedCharacter)
                return data;
        }

        return null;
    }

    private string GetResultDetail(
        MatchSession.RoundResultRecord result)
    {
        switch (matchSession.CurrentRoundResultDetailType)
        {
            case RoundResultDetailType.Status:
                return result.placement == 1
                    ? "ALIVE"
                    : "ELIMINATED";

            case RoundResultDetailType.HoldTime:
                return $"{result.detailValue}s";

            case RoundResultDetailType.Flags:
                return $"{result.detailValue} FLAG" +
                       (result.detailValue == 1 ? "" : "S");

            default:
                return "";
        }
    }

    private void HideGameplayCanvases()
    {
        if (gameplayCanvasVisibility == null)
        {
            gameplayCanvasVisibility =
                FindAnyObjectByType<GameplayCanvasVisibility>();
        }

        if (gameplayCanvasVisibility == null)
        {
            Debug.LogWarning(
                "[MATCH RESULT UI] GameplayCanvasVisibility not found."
            );
            return;
        }

        gameplayCanvasVisibility.HideGameplayCanvases();
        gameplayCanvasesHidden = true;
    }

    private void RestoreGameplayCanvases()
    {
        if (!gameplayCanvasesHidden)
            return;

        if (gameplayCanvasVisibility != null)
            gameplayCanvasVisibility.RestoreGameplayCanvases();

        gameplayCanvasVisibility = null;
        gameplayCanvasesHidden = false;
    }

    private void StopResultCoroutine()
    {
        if (resultCoroutine == null)
            return;

        StopCoroutine(resultCoroutine);
        resultCoroutine = null;
    }

    private void ClearCards()
    {
        foreach (MatchResultCardUI card in resultCards)
        {
            if (card != null)
                Destroy(card.gameObject);
        }

        resultCards.Clear();
    }

    public void HideImmediately()
    {
        StopResultCoroutine();

        if (rootPanel != null)
            rootPanel.SetActive(false);

        ClearCards();
        RestoreGameplayCanvases();
    }

    private void OnDestroy()
    {
        UnbindMatchSession();
    }
}
