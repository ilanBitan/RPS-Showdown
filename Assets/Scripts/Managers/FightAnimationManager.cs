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
    
    [Header("Special Unit Sprites")]
    public Sprite trapSprite;
    public Sprite flagSprite;
    
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
    
    // New overload for special encounters (Trap/Flag)
    public void UpdatePreChoiceWeaponDisplay(RPSUnit.RPSKind playerKind, RPSUnit.UnitRole enemyRole)
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
                enemyImage.sprite = GetSpriteForRole(enemyRole);
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
    
    // New overload for special encounters
    public void UpdateFightDisplaySprites(RPSUnit.RPSKind playerChoice, RPSUnit.UnitRole enemyRole)
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
                enemyImage.sprite = GetSpriteForRole(enemyRole);
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
    
    // Special result animation for trap encounters
    public IEnumerator ShowTrapResult(bool playerSteppedOnTrap)
    {
        fightPanel?.SetActive(true);
        
        Animator playerAnimator = fightPlayer?.GetComponent<Animator>();
        Animator enemyAnimator = fightEnemy?.GetComponent<Animator>();
        
        if (playerSteppedOnTrap)
        {
            // Player loses to trap
            playerAnimator?.SetTrigger("lost");
            enemyAnimator?.SetTrigger("won");
            UnityEngine.Debug.Log("Player stepped on trap!");
        }
        else
        {
            // AI stepped on trap
            playerAnimator?.SetTrigger("won");
            enemyAnimator?.SetTrigger("lost");
            UnityEngine.Debug.Log("AI stepped on trap!");
        }
        
        yield return new WaitForSeconds(2.1f);
        fightPanel?.SetActive(false);
    }
    
    // Special result animation for flag capture
    public IEnumerator ShowFlagCaptureResult(bool playerCapturedFlag)
    {
        fightPanel?.SetActive(true);
        
        Animator playerAnimator = fightPlayer?.GetComponent<Animator>();
        Animator enemyAnimator = fightEnemy?.GetComponent<Animator>();
        
        if (playerCapturedFlag)
        {
            // Player captures flag and wins
            playerAnimator?.SetTrigger("won");
            enemyAnimator?.SetTrigger("lost");
            UnityEngine.Debug.Log("Player captured the flag!");
        }
        else
        {
            // AI captures flag and wins
            playerAnimator?.SetTrigger("lost");
            enemyAnimator?.SetTrigger("won");
            UnityEngine.Debug.Log("AI captured the flag!");
        }
        
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
    
    private Sprite GetSpriteForRole(RPSUnit.UnitRole role)
    {
        switch (role)
        {
            case RPSUnit.UnitRole.Trap: return trapSprite;
            case RPSUnit.UnitRole.Flag: return flagSprite;
            default: return rockSprite; // fallback
        }
    }

    // Add these methods to your FightAnimationManager class

// New overload for when a role (trap/flag) is on player side and unit kind is on enemy side
public void UpdatePreChoiceWeaponDisplay(RPSUnit.UnitRole playerRole, RPSUnit.RPSKind enemyKind)
{
    if (playerWeaponDisplay != null)
    {
        Image playerImage = playerWeaponDisplay.GetComponent<Image>();
        if (playerImage != null)
        {
            playerImage.sprite = GetSpriteForRole(playerRole);
        }
    }
    if (enemyWeaponDisplay != null)
    {
        Image enemyImage = enemyWeaponDisplay.GetComponent<Image>();
        if (enemyImage != null)
        {
            enemyImage.sprite = GetSpriteForChoice(enemyKind);
        }
    }
}

// New overload for fight display sprites
public void UpdateFightDisplaySprites(RPSUnit.UnitRole playerRole, RPSUnit.RPSKind enemyKind)
{
    if (playerWeaponDisplay != null)
    {
        Image playerImage = playerWeaponDisplay.GetComponent<Image>();
        if (playerImage != null)
        {
            playerImage.sprite = GetSpriteForRole(playerRole);
        }
    }
    if (enemyWeaponDisplay != null)
    {
        Image enemyImage = enemyWeaponDisplay.GetComponent<Image>();
        if (enemyImage != null)
        {
            enemyImage.sprite = GetSpriteForChoice(enemyKind);
        }
    }
}





}