using System.Collections;
using UnityEngine;

public class AudioTest : MonoBehaviour
{
    private IEnumerator Start()
    {
        AudioManager.Instance.PlayMusic(MusicID.MainMenu);

        yield return new WaitForSecondsRealtime(5f);

        AudioManager.Instance.PlayMusic(MusicID.Lobby);
    }
}