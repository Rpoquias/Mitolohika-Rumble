
using UnityEngine;

[CreateAssetMenu(
    fileName = "GameModeData",
    menuName = "Mitolohika Rumble/Game Mode Data"
)]
public class GameModeData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private GameModeType gameMode;

    [Header("Display")]
    [SerializeField] private string displayName;
    [SerializeField] private string locationName;

    [Header("Visuals")]
    [SerializeField] private Sprite modeCardImage;
    [SerializeField] private Sprite modeBackground;
    [SerializeField] private Sprite loadingBackground;

    [Header("Scene")]
    [SerializeField] private string scenePath;

    public GameModeType GameMode => gameMode;
    public string DisplayName => displayName;
    public string LocationName => locationName;

    public Sprite ModeCardImage => modeCardImage;
    public Sprite ModeBackground => modeBackground;
    public Sprite LoadingBackground => loadingBackground;

    public string ScenePath => scenePath;
}
