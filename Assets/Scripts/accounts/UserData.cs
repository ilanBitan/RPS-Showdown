using System;
using UnityEngine;

[Serializable]
public class UserData
{
    public string displayName;
    public string email;
    public int score;
    public int wins;
    public int losses;
    public string lastLogin;
    public int rockChoices;    // מספר פעמים שבחר אבן בתיקו
    public int paperChoices;   // מספר פעמים שבחר נייר בתיקו
    public int scissorsChoices; // מספר פעמים שבחר מספריים בתיקו

    public UserData()
    {
        displayName = "";
        email = "";
        score = 0;
        wins = 0;
        losses = 0;
        lastLogin = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}