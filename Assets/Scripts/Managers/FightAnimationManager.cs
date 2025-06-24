using System.Collections;
using UnityEngine;
using UnityEngine.UI;
//seperate after fixing!!!
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
    
    private bool playIntro = true;
   

private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;

        fightPanel?.SetActive(false);
    }

    public void UpdatePreChoiceWeaponDisplay(RPSUnit.RPSKind playerKind, RPSUnit.RPSKind aiKind)
    {
        if (playerWeaponDisplay != null)
        {
            Image playerImage = playerWeaponDisplay.GetComponent<Image>();
            if (playerImage != null)
            {
                playerImage.sprite = GetSpriteForChoice(playerKind);
            }
        }

        if (enemyWeaponDisplay != null)
        {
            Image enemyImage = enemyWeaponDisplay.GetComponent<Image>();
            if (enemyImage != null)
            {
                enemyImage.sprite = GetSpriteForChoice(aiKind);
            }
        }
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

    public void UpdateFightDisplaySprites(RPSUnit.RPSKind playerChoice, RPSUnit.RPSKind aiChoice)
    {
        if (playerWeaponDisplay != null)
        {
            Image playerImage = playerWeaponDisplay.GetComponent<Image>();
            if (playerImage != null)
            {
                playerImage.sprite = GetSpriteForChoice(playerChoice);
            }
        }

        if (enemyWeaponDisplay != null)
        {
            Image enemyImage = enemyWeaponDisplay.GetComponent<Image>();
            if (enemyImage != null)
            {
                enemyImage.sprite = GetSpriteForChoice(aiChoice);
            }
        }
    }

    public IEnumerator ShowFightResult(bool playerWon, bool aiWon)
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
        
        UnityEngine.Debug.Log("Fight result: Player won: " + playerWon + ", AI won: " + aiWon);

        yield return new WaitForSeconds(2.1f);

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