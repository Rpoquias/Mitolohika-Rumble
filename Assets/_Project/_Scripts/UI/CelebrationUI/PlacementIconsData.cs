
using UnityEngine;

[CreateAssetMenu(
    fileName = "PlacementIconsData",
    menuName = "Mitolohika Rumble/Placement Icons Data"
)]
public class PlacementIconsData : ScriptableObject
{
    [Header("Round Placement Icons - Top Row")]
    [SerializeField] private Sprite roundFirst;
    [SerializeField] private Sprite roundSecond;
    [SerializeField] private Sprite roundThird;
    [SerializeField] private Sprite roundFourth;

    [Header("Final Ranking Icons - Bottom Row")]
    [SerializeField] private Sprite finalFirst;
    [SerializeField] private Sprite finalSecond;
    [SerializeField] private Sprite finalThird;
    [SerializeField] private Sprite finalFourth;

    public Sprite GetRoundIcon(int placement)
    {
        switch (placement)
        {
            case 1: return roundFirst;
            case 2: return roundSecond;
            case 3: return roundThird;
            case 4: return roundFourth;
            default: return null;
        }
    }

    public Sprite GetFinalIcon(int placement)
    {
        switch (placement)
        {
            case 1: return finalFirst;
            case 2: return finalSecond;
            case 3: return finalThird;
            case 4: return finalFourth;
            default: return null;
        }
    }
}
