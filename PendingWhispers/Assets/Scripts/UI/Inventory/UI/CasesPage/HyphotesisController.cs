using UnityEngine;
using System.Collections.Generic;
using Inventory.Model;

public class HypothesisController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private HypothesisPanelUI panel;

    private HypothesisData hypothesisData;
    private string[] currentHypothesis;

    public int TotalSlots
    {
        get
        {
            if (hypothesisData == null)
                return 0;

            return hypothesisData.slots.Count;
        }
    }

    private void Awake()
    {
        if (panel != null)
            panel.SetCallback(OnDropdownChanged);
    }

    public void OpenHypothesis()
    {
        var currentCase = CaseManager.Instance.GetCurrentCaseData();

        if (currentCase == null)
        {
            Debug.LogError("No active CaseData");
            return;
        }

        if (currentCase.hypothesis == null)
        {
            Debug.LogError($"Case '{currentCase.caseTitle}' has no HypothesisData assigned");
            return;
        }

        gameObject.SetActive(true);

        hypothesisData = currentCase.hypothesis;

        currentHypothesis = new string[hypothesisData.slots.Count];

        panel.Build(
            hypothesisData.textParts,
            BuildSlotOptions()
        );
    }

    public void CloseHypothesis()
    {
        gameObject.SetActive(false);
    }

    private void OnDropdownChanged(int index, string value)
    {
        if (currentHypothesis == null)
            return;

        if (index < 0 || index >= currentHypothesis.Length)
            return;

        currentHypothesis[index] = value;

        Debug.Log($"Hypothesis Slot {index}: {value}");
    }

    public bool IsCorrect()
    {
        if (hypothesisData == null)
            return false;

        if (currentHypothesis == null)
            return false;

        if (currentHypothesis.Length != hypothesisData.correctAnswers.Count)
            return false;

        for (int i = 0; i < currentHypothesis.Length; i++)
        {
            if (currentHypothesis[i] != hypothesisData.correctAnswers[i])
                return false;
        }

        return true;
    }

    public int GetCorrectCount()
    {
        if (hypothesisData == null)
            return 0;

        if (currentHypothesis == null)
            return 0;

        int score = 0;

        for (int i = 0; i < currentHypothesis.Length; i++)
        {
            if (i >= hypothesisData.correctAnswers.Count)
                continue;

            if (currentHypothesis[i] == hypothesisData.correctAnswers[i])
                score++;
        }

        return score;
    }

    private List<List<string>> BuildSlotOptions()
    {
        var slots = new List<List<string>>();

        if (hypothesisData == null)
            return slots;

        foreach (var slot in hypothesisData.slots)
        {
            slots.Add(BuildOptions(slot.type));
        }

        return slots;
    }

    private List<string> BuildOptions(HypothesisSlotType type)
    {
        var options = new List<string>();

        switch (type)
        {
            case HypothesisSlotType.Person:

                options.Add("Nobody");

                if (PeopleJournalSystem.Instance != null)
                {
                    foreach (var person in PeopleJournalSystem.Instance.GetEntries())
                    {
                        options.Add(person.personName);
                    }
                }

                break;

            case HypothesisSlotType.Item:

                if (InventoryRuntime.Instance != null)
                {
                    var inventory = InventoryRuntime.Instance.GetInventory()
                        .GetCurrentInventoryState();

                    foreach (var kvp in inventory)
                    {
                        if (!kvp.Value.IsEmpty)
                        {
                            options.Add(kvp.Value.item.name);
                        }
                    }
                }

                break;

            case HypothesisSlotType.Clue:

                if (InventoryRuntime.Instance != null)
                {
                    var items = InventoryRuntime.Instance.GetInventory()
                        .GetItemsByType(ItemType.Clue);

                    foreach (var item in items)
                    {
                        options.Add(item.item.name);
                    }
                }

                break;
        }

        return options;
    }

    public string GetHypothesisText()
    {
        if (hypothesisData == null)
            return "";

        if (currentHypothesis == null)
            return "";

        string result = "";

        for (int i = 0; i < hypothesisData.slots.Count; i++)
        {
            result += hypothesisData.textParts[i];

            if (i < currentHypothesis.Length &&
                !string.IsNullOrEmpty(currentHypothesis[i]))
            {
                result += currentHypothesis[i];
            }
        }

        result += hypothesisData.textParts[^1];

        return result;
    }
}