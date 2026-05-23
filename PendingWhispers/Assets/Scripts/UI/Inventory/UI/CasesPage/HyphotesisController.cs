using UnityEngine;
using System.Collections.Generic;
using Inventory.Model;

public class HypothesisController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private HypothesisPanelUI panel;
    [SerializeField] private List<HypothesisSlotDefinition> slotDefinitions;

    private void Awake()
    {
        panel.SetCallback(OnDropdownChanged);
    }

    public void OpenHypothesis()
    {
        gameObject.SetActive(true);

        var textParts = new List<string>
        {
            "El culpable fue ",
            " usando ",
            " en ",
            ""
        };

        panel.Build(textParts, BuildSlotOptions());
    }

    public void CloseHypothesis()
    {
        gameObject.SetActive(false);
    }

    public void OnDropdownChanged(int index, string value)
    {
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
}