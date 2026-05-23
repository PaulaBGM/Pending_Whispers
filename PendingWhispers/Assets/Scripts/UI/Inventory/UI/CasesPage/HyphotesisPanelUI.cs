using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class HypothesisPanelUI : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private TMP_Text textPrefab;
    [SerializeField] private TMP_Dropdown dropdownPrefab;

    [Header("Container")]
    [SerializeField] private Transform container;

    private readonly List<GameObject> spawned = new();

    public void Build(string template, List<List<string>> slotOptions)
    {
        Clear();

        string[] parts = SplitSafe(template, slotOptions.Count);

        for (int i = 0; i < slotOptions.Count; i++)
        {
            AddText(parts[i]);

            var dd = Instantiate(dropdownPrefab, container);
            SetupDropdown(dd, i, slotOptions[i]);

            spawned.Add(dd.gameObject);
        }

        AddText(parts[^1]);
    }

    // =========================
    // DROPDOWN SETUP
    // =========================
    private void SetupDropdown(TMP_Dropdown dropdown, int index, List<string> options)
    {
        dropdown.ClearOptions();
        dropdown.AddOptions(options);

        dropdown.onValueChanged.AddListener(i =>
        {
            string value = dropdown.options[i].text;

            // Evento directo simple (sin HypothesisEvents si no lo necesitas)
            FindObjectOfType<HypothesisController>()
                .OnDropdownChanged(index, value);
        });
    }

    // =========================
    // TEXT CREATION
    // =========================
    private void AddText(string text)
    {
        var t = Instantiate(textPrefab, container);
        t.text = text;
        spawned.Add(t.gameObject);
    }

    // =========================
    // SAFE SPLIT (IMPORTANTE)
    // =========================
    private string[] SplitSafe(string template, int slots)
    {
        string[] result = new string[slots + 1];

        int last = 0;

        for (int i = 0; i < slots; i++)
        {
            string key = "{" + i + "}";
            int idx = template.IndexOf(key);

            if (idx == -1)
            {
                result[i] = template.Substring(last);
                last = template.Length;
                continue;
            }

            result[i] = template.Substring(last, idx - last);
            last = idx + key.Length;
        }

        result[slots] = template.Substring(last);

        return result;
    }

    // =========================
    // CLEANUP
    // =========================
    private void Clear()
    {
        foreach (var go in spawned)
            Destroy(go);

        spawned.Clear();
    }
}