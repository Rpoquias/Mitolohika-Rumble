using UnityEngine;

public class SceneAudio : MonoBehaviour
{
    [Header("Scene Audio")]
    [SerializeField] private MusicID music = MusicID.None;
    [SerializeField] private SFXID enterSound = SFXID.None;

    private void Start()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("[SceneAudio] AudioManager instance not found.");
            return;
        }

        if (music != MusicID.None)
            AudioManager.Instance.PlayMusic(music);

        if (enterSound != SFXID.None)
            AudioManager.Instance.PlaySFX(enterSound);
    }
}