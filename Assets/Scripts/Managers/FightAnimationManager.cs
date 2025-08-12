using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages all fight-related animations and sprite updates in the game, including
/// standard RPS battles, trap encounters, and flag captures.
/// </summary>
public class FightAnimationManager : MonoBehaviour
{
    // Singleton instance for global access
    public static FightAnimationManager Instance;
    // Main panel that contains the fight animation
    public GameObject fightPanel;
    // Player and enemy GameObjects that display the fight animations
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
    
    /// <summary>
    /// Updates the weapon display sprites before a choice is made (for running animations).
    /// </summary>
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
    
    /// <summary>
    /// Plays the initial fight intro animation when a battle starts.
    /// Shows the fight panel for 1.2 seconds with a "run" animation trigger.
    /// </summary>
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
    
    // after a tie, the sprites need to be updated to show the current choices
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
    
    /// <summary>
    /// Displays the result of a standard RPS battle.
    /// Shows a 2.1 second animation where either the player or AI character
    /// performs a victory animation while the other performs a defeat animation.
    /// </summary>
    /// <param name="playerWon">True if the player won the battle</param>
    /// <param name="aiWon">True if the AI won the battle</param>
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
    
    /// <summary>
    /// Displays the result animation when a trap is triggered.
    /// Shows a 2.1 second animation sequence where the unit that stepped on the trap
    /// performs a defeat animation while the other unit performs a victory animation.
    /// </summary>
    /// <param name="playerSteppedOnTrap">True if the player triggered the trap, false if AI triggered it</param>
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
    
    /// <summary>
    /// Displays the result animation when a flag is captured.
    /// Shows a 2.1 second animation where the unit that captured the flag
    /// performs a victory animation while the other unit performs a defeat animation.
    /// This represents a game-ending victory condition.
    /// </summary>
    /// <param name="playerCapturedFlag">True if the player captured the flag, false if AI captured it</param>
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
    
    /// <summary>
    /// Returns the appropriate sprite for a given RPS choice (Rock, Paper, or Scissors).
    /// Used to display the unit's weapon choice during battle animations.
    /// Falls back to rock sprite if an invalid choice is provided.
    /// </summary>
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
    
    /// <summary>
    /// Returns the appropriate sprite for special unit roles (Trap or Flag).
    /// Used to display special unit types during encounters with traps or flags.
    /// Falls back to rock sprite if an invalid role is provided.
    /// </summary>
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