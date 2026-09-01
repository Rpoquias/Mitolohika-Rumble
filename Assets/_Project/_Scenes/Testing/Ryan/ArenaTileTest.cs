using UnityEngine;

public class ArenaTileTest : MonoBehaviour
{
    [SerializeField] private ArenaShrinkController arenaShrinkController;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            arenaShrinkController.ShrinkRandomSide();
        }
    }
}