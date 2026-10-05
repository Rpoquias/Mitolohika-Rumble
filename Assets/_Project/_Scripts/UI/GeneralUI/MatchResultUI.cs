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

    [Header("Timing")]
    [SerializeField] private float resultDisplayDuration = 3f;

    private MatchSession matchSession;

    private readonly List<MatchResultCardUI> resultCards =
        new List<MatchResultCardUI>();

    private Coroutine resultCoroutine;

    private int lastHandledSequence = -1;

    public event Action OnResultCompleted;

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (rootPanel != null)
        {
            rootPanel.SetActive(false);
        }

        if (roundInfoObject != null)
        {
            roundInfoObject.SetActive(true);
        }
    }

    private void Start()
    {
        CreateSessionReference();
    }

    private void Update()
    {
        if (matchSession == null)
        {
            CreateSessionReference();
            return;
        }

        // Fallback in case the event was missed.
        if (matchSession.RoundResultSequence !=
            lastHandledSequence)
        {
            HandleRoundResult();
        }
    }

    // ============================================================
    // MATCH SESSION
    // ============================================================

    private void CreateSessionReference()
    {
        if (matchSession != null)
            return;

        matchSession =
            MatchSession.Instance;

        if (matchSession == null)
            return;

        matchSession.OnRoundResultRequested +=
            HandleRoundResult;

        CheckForMissedResult();
    }

    private void CheckForMissedResult()
    {
        if (matchSession == null)
            return;

        if (matchSession.RoundResultSequence <= 0)
            return;

        if (matchSession.RoundResultSequence ==
            lastHandledSequence)
        {
            return;
        }

        HandleRoundResult();
    }

    // ============================================================
    // RESULT
    // ============================================================

    private void HandleRoundResult()
    {
        if (matchSession == null)
            return;

        if (matchSession.RoundResultSequence <= 0)
            return;

        lastHandledSequence =
            matchSession.RoundResultSequence;

        if (resultCoroutine != null)
        {
            StopCoroutine(
                resultCoroutine
            );
        }

        resultCoroutine =
            StartCoroutine(
                ShowResultCoroutine()
            );
    }

    private IEnumerator ShowResultCoroutine()
    {
        if (rootPanel != null)
        {
            rootPanel.SetActive(true);
        }

        ClearCards();

        // --------------------------------------------------------
        // Round number
        // --------------------------------------------------------

        if (roundInfoObject != null)
        {
            roundInfoObject.SetActive(true);
        }

        if (roundText != null)
        {
            roundText.text =
                $"ROUND {matchSession.CurrentRound} RESULTS";
        }

        // --------------------------------------------------------
        // Get results
        // --------------------------------------------------------

        List<MatchSession.RoundResultRecord> results =
            new List<MatchSession.RoundResultRecord>();

        for (int i = 0;
             i < matchSession.CurrentRoundResultCount;
             i++)
        {
            if (matchSession.TryGetCurrentRoundResult(
                    i,
                    out MatchSession.RoundResultRecord result))
            {
                results.Add(result);
            }
        }

        results.Sort(
            (a, b) =>
                a.placement.CompareTo(
                    b.placement
                )
        );

        // --------------------------------------------------------
        // Create cards
        // --------------------------------------------------------

        foreach (
            MatchSession.RoundResultRecord result
            in results)
        {
            MatchResultCardUI card =
                Instantiate(
                    resultCardPrefab,
                    resultContainer
                );
card.Setup(
    result.placement,
    result.player,
    GetResultDetail(result)
);

            resultCards.Add(card);
        }

        // --------------------------------------------------------
        // Display duration
        // --------------------------------------------------------

        yield return new WaitForSeconds(
            resultDisplayDuration
        );

        HideImmediately();

        OnResultCompleted?.Invoke();
    }

    // ============================================================
    // CLEANUP
    // ============================================================

    private void ClearCards()
    {
        foreach (MatchResultCardUI card
                 in resultCards)
        {
            if (card != null)
            {
                Destroy(
                    card.gameObject
                );
            }
        }

        resultCards.Clear();
    }

private string GetResultDetail(
    MatchSession.RoundResultRecord result)
{
    switch (
        matchSession.CurrentRoundResultDetailType)
    {
        case RoundResultDetailType.Status:

            return result.placement == 1
                ? "ALIVE"
                : "ELIMINATED";

        case RoundResultDetailType.HoldTime:

            return $"{result.detailValue}s";

        case RoundResultDetailType.Flags:

            return
                $"{result.detailValue} FLAG" +
                (result.detailValue == 1
                    ? ""
                    : "S");

        default:

            return "";
    }
}
    public void HideImmediately()
    {
        if (resultCoroutine != null)
        {
            StopCoroutine(
                resultCoroutine
            );

            resultCoroutine = null;
        }

        if (rootPanel != null)
        {
            rootPanel.SetActive(false);
        }

        ClearCards();
    }

    private void OnDestroy()
    {
        if (matchSession != null)
        {
            matchSession.OnRoundResultRequested -=
                HandleRoundResult;
        }
    }
}