using UnityEngine;
using System.Collections.Generic;
using Inventory.Model;

public class HypothesisController : MonoBehaviour
{
    [Header("Sentence")]
    [SerializeField] private List<string> textParts;
    
    [Header("UI")]
    [SerializeField] private HypothesisPanelUI panel;
    [SerializeField] private List<HypothesisSlotDefinition> slotDefinitions;

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
        panel.SetCallback(OnDropdownChanged);
    }

    public void OpenHypothesis()
    {
        gameObject.SetActive(true);

        var currentCase = CaseManager.Instance.GetCurrentCaseData();

        if (currentCase == null || currentCase.hypothesis == null)
        {
            Debug.LogError("Case has no HypothesisData");
            return;
        }

        hypothesisData = currentCase.hypothesis;

        currentHypothesis = new string[hypothesisData.slots.Count];

        panel.Build(
            hypothesisData.textParts,
            BuildSlotOptions()
        );
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
            {
                return false;
            }
        }

        return true;
    }
    public void CloseHypothesis()
    {
        gameObject.SetActive(false);
    }
    
    public void OnDropdownChanged(int index, string value)
    {
        currentHypothesis[index] = value;

        Debug.Log($"Slot {index}: {value}");
    }
    public int GetCorrectCount()
    {
        if (hypothesisData == null)
            return 0;

        int score = 0;

        for (int i = 0; i < currentHypothesis.Length; i++)
        {
            if (currentHypothesis[i] == hypothesisData.correctAnswers[i])
            {
                score++;
            }
        }

        return score;
    }
    private List<List<string>> BuildSlotOptions()
    {
        var slots = new List<List<string>>();

        for (int i = 0; i < hypothesisData.slots.Count; i++)
        {
            slots.Add(BuildOptions(hypothesisData.slots[i].type));
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

                foreach (var person in PeopleJournalSystem.Instance.GetEntries())
                    options.Add(person.personName);
                break;
                

            case HypothesisSlotType.Item:
                var inventory = InventoryRuntime.Instance.GetInventory()
                    .GetCurrentInventoryState();

                foreach (var kvp in inventory)
                    if (!kvp.Value.IsEmpty)
                        options.Add(kvp.Value.item.name);
                break;

            case HypothesisSlotType.Clue:
                var items = InventoryRuntime.Instance.GetInventory()
                    .GetItemsByType(ItemType.Clue);

                foreach (var item in items)
                    options.Add(item.item.name);
                break;
        }

        return options;
    }

    public string GetHypothesisText()
    {
        if (hypothesisData == null)
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