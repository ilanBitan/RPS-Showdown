using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the visual representation of RPS units.
/// Originally, units were represented by text (R/P/S/F/T),
/// now enhanced with sprite-based weapon images for better visuals.
/// </summary>
public class UnitVisualController : MonoBehaviour
{
    private RPSUnit rpsUnit;

    public Image weaponImage;

    public Sprite rockSprite;
    public Sprite paperSprite;
    public Sprite scissorsSprite;
    public Sprite flagSprite;
    public Sprite trapSprite;

    private void Awake()
    {
        // Get the RPSUnit component this visual controller is attached to
        rpsUnit = GetComponent<RPSUnit>();

        // If weaponImage wasn't assigned in inspector, try to find it in children
        if (weaponImage == null)
        {
            weaponImage = GetComponentInChildren<Image>(true);
        }

        // Initially hide the weapon image
        // In the original text system, this would hide the letter
        // Now it hides the weapon sprite until the unit is revealed
        if (weaponImage != null)
            weaponImage.gameObject.SetActive(false);
    }

    public void UpdateWeaponVisual(string letter)
    {
        if (weaponImage == null) return;

        if (string.IsNullOrEmpty(letter))
        {
            weaponImage.gameObject.SetActive(false);
            return;
        }

        // Show the weapon image component
        weaponImage.gameObject.SetActive(true);

        // Convert letter code to corresponding sprite
        // This maintains compatibility with the original text-based system
        // while providing enhanced visual representation
        switch (letter)
        {
            case "R": // Rock
                weaponImage.sprite = rockSprite;
                break;
            case "P": // Paper
                weaponImage.sprite = paperSprite;
                break;
            case "S": // Scissors
                weaponImage.sprite = scissorsSprite;
                break;
            case "F": // Flag
                weaponImage.sprite = flagSprite;
                break;
            case "T": // Trap
                weaponImage.sprite = trapSprite;
                break;
            default: // Invalid or unknown type
                weaponImage.gameObject.SetActive(false);
                break;
        }
    }
}
