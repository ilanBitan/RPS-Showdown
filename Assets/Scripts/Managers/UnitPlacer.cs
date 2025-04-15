using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class UnitPlacer : MonoBehaviour
{
    public GameObject playerUnitPrefab;
    public GameObject enemyUnitPrefab;
    public Transform boardTransform;

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
        int index = row * columns + col;
        if (index >= boardTransform.childCount) return;

        Transform tile = boardTransform.GetChild(index);
        GameObject unit = Instantiate(prefab, tile);
        unit.name = $"Unit_{row}_{col}";

        RectTransform unitRect = unit.GetComponent<RectTransform>();
        unitRect.anchorMin = unitRect.anchorMax = unitRect.pivot = new Vector2(0.5f, 0.5f);
        unitRect.anchoredPosition = Vector2.zero;
        unitRect.sizeDelta = new Vector2(100, 100);

        Image img = unit.GetComponent<Image>();
        if (img != null)
        {
            Color c = img.color;
            c.a = 0f;
            img.color = c;
            StartCoroutine(FadeIn(img, 0.5f));
        }

        RPSUnit rps = unit.GetComponent<RPSUnit>();
        if (rps != null)
        {
            rps.Position = new Vector2Int(col, row);
            rps.IsPlayerControlled = (playerId == 1);
            rps.playerId = playerId;

            rps.role = RPSUnit.UnitRole.None;
            rps.Kind = RPSUnit.RPSKind.Rock; // יוחלף בעתיד בהגרלה
            rps.ResetVisual(); // << החשוב כאן!

            if (playerId == 1) player1Units.Add(rps);
            else player2Units.Add(rps);
        }
    }

    IEnumerator FadeIn(Image img, float duration)
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
