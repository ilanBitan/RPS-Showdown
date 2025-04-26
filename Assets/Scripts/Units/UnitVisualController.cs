using UnityEngine;
using UnityEngine.UI;

public class UnitVisualController : MonoBehaviour
{
    // Reference to the RPSUnit this visual controller is attached to
    private RPSUnit rpsUnit;
    
    // Reference to the image component that will display the weapon
    public Image weaponImage;
    
    // Sprite references
    public Sprite rockSprite;
    public Sprite paperSprite;
    public Sprite scissorsSprite;
    public Sprite flagSprite;
    public Sprite trapSprite;
    
    private void Awake()
    {
        // Get the RPSUnit component
        rpsUnit = GetComponent<RPSUnit>();
        
        // If weaponImage isn't assigned, try to find it
        if (weaponImage == null)
        {
            // Try to find an Image component in children
            // Note: This might find the wrong image if you have multiple images
            weaponImage = GetComponentInChildren<Image>(true);
            
            // Alternative: Find by specific name
            // weaponImage = transform.Find("WeaponImage").GetComponent<Image>();
        }
    }
    
    // Update is called once per frame
    private void Update()
    {
        // This continuously updates the visual, but it's the simplest approach
        // without modifying the original RPSUnit class
        UpdateWeaponVisual();
    }
    
    public void UpdateWeaponVisual()
    {
        if (weaponImage == null || rpsUnit == null) return;
        
        // Get the current letter being displayed by the RPSUnit
        string currentLetter = rpsUnit.GetLetter();
        
        // If the letter is empty, hide the weapon image
        if (string.IsNullOrEmpty(currentLetter))
        {
            weaponImage.gameObject.SetActive(false);
            return;
        }
        
        // Show the weapon image
        weaponImage.gameObject.SetActive(true);
        
        // Set the appropriate sprite based on the current letter
        switch (currentLetter)
        {
            case "R":
                weaponImage.sprite = rockSprite;
                break;
            case "P":
                weaponImage.sprite = paperSprite;
                break;
            case "S":
                weaponImage.sprite = scissorsSprite;
                break;
            case "F":
                weaponImage.sprite = flagSprite;
                break;
            case "T":
                weaponImage.sprite = trapSprite;
                break;
            default:
                // Hide the image if no recognizable letter
                weaponImage.gameObject.SetActive(false);
                break;
        }
    }
}