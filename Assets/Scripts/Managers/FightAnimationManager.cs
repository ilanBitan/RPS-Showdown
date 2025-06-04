using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FightAnimationManager : MonoBehaviour
{
    public static FightAnimationManager Instance;

    public GameObject fightPanel;
    public GameObject fightPlayer;
    public GameObject fightEnemy;

    public Sprite rockSprite;
    public Sprite paperSprite;
    public Sprite scissorsSprite;

    public GameObject playerWeaponDisplay;
    public GameObject enemyWeaponDisplay;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    public IEnumerator PlayFightIntroAnimation()
    {
        fightPanel?.SetActive(true);

        Animator anim = fightPanel.GetComponent<Animator>();
        if (anim != null)
            anim.SetTrigger("run");

        yield return new WaitForSeconds(1.2f);

        fightPanel?.SetActive(false);
    }

    public void UpdateWeaponDisplays(RPSUnit.RPSKind playerChoice, RPSUnit.RPSKind aiChoice)
    {
        // Update player weapon display
        if (playerWeaponDisplay != null)
        {
            Image playerImage = playerWeaponDisplay.GetComponent<Image>();
            if (playerImage != null)
            {
                playerImage.sprite = GetSpriteForChoice(playerChoice);
            }
        }

        // Update enemy weapon display
        if (enemyWeaponDisplay != null)
        {
            Image enemyImage = enemyWeaponDisplay.GetComponent<Image>();
            if (enemyImage != null)
            {
                enemyImage.sprite = GetSpriteForChoice(aiChoice);
            }
        }
    }

    public IEnumerator PlayFightResultAnimation(bool playerWon, bool aiWon)
    {
        fightPanel?.SetActive(true);

        Animator playerAnimator = fightPlayer?.GetComponent<Animator>();
        Animator enemyAnimator = fightEnemy?.GetComponent<Animator>();

        if (playerWon)
        {
            playerAnimator?.SetTrigger("won");
            enemyAnimator?.SetTrigger("lost");
        }
        else if (aiWon)
        {
            playerAnimator?.SetTrigger("lost");
            enemyAnimator?.SetTrigger("won");
        }

        yield return new WaitForSeconds(2f);

        fightPanel?.SetActive(false);
    }

    private Sprite GetSpriteForChoice(RPSUnit.RPSKind choice)
    {
        switch (choice)
        {
            case RPSUnit.RPSKind.Rock: return rockSprite;
            case RPSUnit.RPSKind.Paper: return paperSprite;
            case RPSUnit.RPSKind.Scissors: return scissorsSprite;
            default: return rockSprite;
        }
    }
}