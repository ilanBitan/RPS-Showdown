using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UnitPlacer : MonoBehaviour
{
    public GameObject playerUnitPrefab;
    public GameObject enemyUnitPrefab;
    public Transform boardTransform; // גרור לכאן את Board

    public int rows = 6;
    public int columns = 7;

    void Start()
    {
        PlaceUnits();
    }

    void PlaceUnits()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (row == 0 || row == 1)
                    CreateUnit(enemyUnitPrefab, row, col);
                else if (row == rows - 2 || row == rows - 1)
                    CreateUnit(playerUnitPrefab, row, col);
            }
        }
    }

    void CreateUnit(GameObject prefab, int row, int col)
    {
        int index = row * columns + col;
        if (index >= boardTransform.childCount)
        {
            Debug.LogWarning($"❗ Index {index} out of bounds for row {row}, col {col}");
            return;
        }

        Transform tile = boardTransform.GetChild(index);
        GameObject unit = Instantiate(prefab, tile);
        unit.name = $"Unit_{row}_{col}";

        RectTransform unitRect = unit.GetComponent<RectTransform>();
        unitRect.anchorMin = new Vector2(0.5f, 0.5f);
        unitRect.anchorMax = new Vector2(0.5f, 0.5f);
        unitRect.pivot = new Vector2(0.5f, 0.5f);
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
            rps.IsPlayerControlled = (row == rows - 2 || row == rows - 1);
        }
    }

    IEnumerator FadeIn(Image img, float duration)
    {
        float time = 0f;
        Color start = img.color;
        Color target = start;
        target.a = 1f;

        while (time < duration)
        {
            img.color = Color.Lerp(start, target, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        img.color = target;
    }
}
