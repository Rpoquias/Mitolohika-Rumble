
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerActionHUD : MonoBehaviour
{
    [System.Serializable]
    private class ActionButton
    {
        public Image buttonImage;
        public TMP_Text cooldownText;
    }


    [Header("Buttons")]
    [SerializeField] private ActionButton jumpButton;
    [SerializeField] private ActionButton bumpButton;
    [SerializeField] private ActionButton abilityButton;

    [Header("Appearance")]
    [SerializeField] private Color availableColor = Color.white;
    [SerializeField] private Color unavailableColor =
        new Color(0.35f, 0.35f, 0.35f, 1f);

    [Header("Refresh")]
    [SerializeField, Min(0.03f)]
    private float refreshInterval = 0.1f;

    private PlayerMovement movement;
    private PlayerBumpAttack bump;
    private CharacterAbility ability;

    private float nextRefreshTime;

    public void SetPlayer(
        PlayerMovement playerMovement,
        PlayerBumpAttack playerBump,
        CharacterAbility playerAbility)
    {
        movement = playerMovement;
        bump = playerBump;
        ability = playerAbility;

        RefreshButtons();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefreshTime)
            return;

        nextRefreshTime = Time.unscaledTime + refreshInterval;

        RefreshButtons();
    }



public void ClearPlayer()
{
    movement = null;
    bump = null;
    ability = null;

    RefreshButtons();
}

  
private void RefreshButtons()
{
    bool movementValid =
        movement != null &&
        movement.Object != null &&
        movement.Object.IsValid &&
        movement.Object.IsInSimulation;

    bool bumpValid =
        bump != null &&
        bump.Object != null &&
        bump.Object.IsValid &&
        bump.Object.IsInSimulation;

    bool abilityValid =
        ability != null &&
        ability.Object != null &&
        ability.Object.IsValid &&
        ability.Object.IsInSimulation;

    bool canJump = false;

    if (movementValid)
    {
        canJump =
            movement.IsGrounded &&
            movement.CanJump &&
            movement.CanMove &&
            !movement.IsBusy &&
            !movement.IsStunned &&
            !movement.IsSnareRooted;
    }

    bool canBump =
        bumpValid && bump.IsBumpAvailable;

    bool canUseAbility =
        abilityValid && ability.IsAbilityAvailable;

    SetButton(jumpButton, canJump, 0f, false);

    SetButton(
        bumpButton,
        canBump,
        bumpValid ? bump.CooldownRemaining : 0f,
        true
    );

    SetButton(
        abilityButton,
        canUseAbility,
        abilityValid ? ability.CooldownRemaining : 0f,
        true
    );
}

    private void SetButton(
        ActionButton button,
        bool available,
        float cooldown,
        bool showCooldown)
    {
        if (button == null)
            return;

        if (button.buttonImage != null)
        {
            button.buttonImage.color =
                available ? availableColor : unavailableColor;
        }

        if (button.cooldownText != null)
        {
            bool showText =
                showCooldown &&
                cooldown > 0.05f;

            button.cooldownText.gameObject.SetActive(showText);

            if (showText)
                button.cooldownText.text =
                    Mathf.CeilToInt(cooldown).ToString();
        }
    }
}