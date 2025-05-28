using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;

public class UnitPlacer : MonoBehaviour
{
    public GameObject playerUnitPrefab;
    public GameObject enemyUnitPrefab;

    public int rows = 6;
    public int columns = 7;

    private List<RPSUnit> player1Units = new List<RPSUnit>();
    private List<RPSUnit> player2Units = new List<RPSUnit>();

    void Start()
    {
        PlaceUnits();
        GameSetupManager.Instance.StartSetup(player1Units, player2Units);
    }

    void PlaceUnits()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (row == 0 || row == 1)
                    CreateUnit(enemyUnitPrefab, row, col, 2);
                else if (row == rows - 2 || row == rows - 1)
                    CreateUnit(playerUnitPrefab, row, col, 1);
            }
        }
    }

    void CreateUnit(GameObject prefab, int row, int col, int playerId)
    {
        GameObject unitObj = Instantiate(prefab);
        unitObj.name = $"Unit_{playerId}_{row}_{col}";

        RPSUnit rps = unitObj.GetComponent<RPSUnit>();
        if (rps != null)
        {
            rps.IsPlayerControlled = (playerId == 1);
            rps.playerId = playerId;
            rps.role = RPSUnit.UnitRole.None;
            rps.Kind = RPSUnit.RPSKind.Rock; // will be randomized later
            rps.ResetVisual();

            Vector2Int pos = new Vector2Int(col, row);
            BoardManager.Instance.PlaceUnit(rps, pos);

            UnityEngine.Debug.Log($"✅ Placed {unitObj.name} for Player {playerId} at [col {col}, row {row}]");

            UnityEngine.UI.Image img = unitObj.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                Color c = img.color;
                c.a = 0f;
                img.color = c;
                StartCoroutine(FadeIn(img, 0.5f));
            }

            if (playerId == 1) player1Units.Add(rps);
            else player2Units.Add(rps);
        }
        else
        {
            UnityEngine.Debug.LogError($"❌ Failed to get RPSUnit component on {unitObj.name}");
        }
    }

    IEnumerator FadeIn(UnityEngine.UI.Image img, float duration)
    {
        float time = 0f;
        Color start = img.color;
        Color target = start; target.a = 1f;

        while (time < duration)
        {
            img.color = Color.Lerp(start, target, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        img.color = target;
    }
}