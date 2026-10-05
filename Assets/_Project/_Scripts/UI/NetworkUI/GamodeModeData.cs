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
    [SerializeField] private Sprite modeIcon;
    [SerializeField] private Sprite modeBackground;

    [Header("Scene")]
    [SerializeField] private string scenePath;

    public GameModeType GameMode => gameMode;
    public string DisplayName => displayName;
    public string LocationName => locationName;
    public Sprite ModeIcon => modeIcon;
    public Sprite ModeBackground => modeBackground;
    public string ScenePath => scenePath;
}