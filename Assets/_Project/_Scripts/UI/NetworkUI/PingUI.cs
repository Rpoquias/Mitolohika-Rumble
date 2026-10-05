using UnityEngine;
using TMPro;
using Fusion;

public class PingUI : MonoBehaviour
{
    [SerializeField] private TMP_Text pingText;
    [SerializeField] private float updateInterval = 0.5f;

    [Header("Color thresholds (ms)")]
    [SerializeField] private int goodThreshold = 80;
    [SerializeField] private int okThreshold = 150;

    private NetworkRunner runner;
    private float timer;

    private void Update()
    {
        if (runner == null)
        {
            runner = FindAnyObjectByType<NetworkRunner>();
        }

        if (runner == null ||
            !runner.IsRunning ||
            !runner.IsConnectedToServer)
        {
            pingText.text = "Ping: --";
            return;
        }

        timer += Time.unscaledDeltaTime;

        if (timer < updateInterval)
            return;

        timer = 0f;

        // Host does not need a remote RTT measurement.
        if (runner.IsServer)
        {
            pingText.text = "Ping: --";
            return;
        }

        try
        {
            double rttSeconds =
                runner.GetPlayerRtt(runner.LocalPlayer);

            int ms = Mathf.RoundToInt(
                (float)rttSeconds * 1000f
            );

            pingText.text = $"Ping: {ms}ms";

            pingText.color =
                ms <= goodThreshold ? Color.green :
                ms <= okThreshold ? Color.yellow :
                Color.red;
        }
        catch
        {
            // RTT is not available yet.
            pingText.text = "Ping: --";
        }
    }
}