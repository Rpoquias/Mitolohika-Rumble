using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour
{
    [SerializeField] private SFXID sound = SFXID.ButtonClick;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        _button.onClick.AddListener(HandleClick);
    }

    private void OnDisable()
    {
        _button.onClick.RemoveListener(HandleClick);
    }

    private void HandleClick()
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySFX(sound);
    }
}