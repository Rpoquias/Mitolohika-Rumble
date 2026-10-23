
using UnityEngine;

public class GameplayCanvasVisibility : MonoBehaviour
{
    [Header("Canvases to hide during round results")]
    [SerializeField] private GameObject mobileCanvas;
    [SerializeField] private GameObject sceneUICanvas;

    private bool mobileCanvasWasActive;
    private bool sceneUICanvasWasActive;
    private bool isHidden;

    public void HideGameplayCanvases()
    {
        if (isHidden)
            return;

        isHidden = true;

        mobileCanvasWasActive =
            mobileCanvas != null && mobileCanvas.activeSelf;

        sceneUICanvasWasActive =
            sceneUICanvas != null && sceneUICanvas.activeSelf;

        if (mobileCanvas != null)
            mobileCanvas.SetActive(false);

        if (sceneUICanvas != null)
            sceneUICanvas.SetActive(false);
    }

    public void RestoreGameplayCanvases()
    {
        if (!isHidden)
            return;

        isHidden = false;

        if (mobileCanvas != null)
            mobileCanvas.SetActive(mobileCanvasWasActive);

        if (sceneUICanvas != null)
            sceneUICanvas.SetActive(sceneUICanvasWasActive);
    }

    private void OnDisable()
    {
        RestoreGameplayCanvases();
    }

    private void OnDestroy()
    {
        RestoreGameplayCanvases();
    }
}
