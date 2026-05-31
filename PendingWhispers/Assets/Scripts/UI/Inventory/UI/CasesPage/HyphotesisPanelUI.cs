using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;

public class HypothesisPanelUI : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private TMP_Text textPrefab;
    [SerializeField] private TMP_Dropdown dropdownPrefab;

    [Header("Row Containers")]
    [SerializeField] private Transform[] rowContainers;

    private readonly List<GameObject> spawned = new();

    private Action<int, string> onValueChanged;

    public void SetCallback(Action<int, string> callback)
    {
        onValueChanged = callback;
    }

    public void Build(List<string> textParts, List<List<string>> slotOptions)
    {
        Clear();

        int slotCount = slotOptions.Count;

        for (int i = 0; i < slotCount; i++)
        {
            if (i >= rowContainers.Length)
            {
                Debug.LogError("Not enough rowContainers!");
                return;
            }

            Transform row = rowContainers[i];

            SpawnText(row, textParts[i]);

            var dd = Instantiate(dropdownPrefab, row);
            SetupDropdown(dd, i, slotOptions[i]);

            spawned.Add(dd.gameObject);
        }

        if (slotCount < rowContainers.Length && slotCount < textParts.Count)
        {
            SpawnText(rowContainers[slotCount], textParts[slotCount]);
        }
    }

    private void SetupDropdown(TMP_Dropdown dropdown, int index, List<string> options)
    {
        dropdown.ClearOptions();
        dropdown.AddOptions(options);

        dropdown.onValueChanged.AddListener(i =>
        {
            string value = dropdown.options[i].text;
            onValueChanged?.Invoke(index, value);
        });
    }

    private void SpawnText(Transform container, string text)
    {
        var t = Instantiate(textPrefab, container);
        t.text = text;
        spawned.Add(t.gameObject);
    }

    private void Clear()
    {
        foreach (var go in spawned)
            Destroy(go);

        spawned.Clear();
    }
}