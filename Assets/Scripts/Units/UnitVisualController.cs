using UnityEngine;
using UnityEngine.UI;

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
        rpsUnit = GetComponent<RPSUnit>();

        if (weaponImage == null)
        {
            weaponImage = GetComponentInChildren<Image>(true);
        }

        // At start, hide the weapon
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

        weaponImage.gameObject.SetActive(true);

        switch (letter)
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
                weaponImage.gameObject.SetActive(false);
                break;
        }
    }
}
