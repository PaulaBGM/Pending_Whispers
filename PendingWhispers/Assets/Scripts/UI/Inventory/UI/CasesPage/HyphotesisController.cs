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
    
    private string[] currentHypothesis;

    private void Awake()
    {
        panel.SetCallback(OnDropdownChanged);
    }

    public void OpenHypothesis()
    {
        gameObject.SetActive(true);
        
        currentHypothesis = new string[slotDefinitions.Count];

        panel.Build(textParts, BuildSlotOptions());
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

    private List<List<string>> BuildSlotOptions()
    {
        var slots = new List<List<string>>();

        for (int i = 0; i < slotDefinitions.Count; i++)
        {
            slots.Add(BuildOptions(slotDefinitions[i].type));
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
        string result = "";

        for (int i = 0; i < slotDefinitions.Count; i++)
        {
            result += textParts[i];

            if (i < currentHypothesis.Length && currentHypothesis[i] != null)
                result += currentHypothesis[i];
        }

        result += textParts[^1];

        return result;
    }
}