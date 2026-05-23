using UnityEngine;
using System.Collections.Generic;
using Inventory.Model;

public class HypothesisController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private HypothesisPanelUI panel;
    [SerializeField] private List<HypothesisSlotDefinition> slotDefinitions;

    private void Start()
    {
        gameObject.SetActive(false);
    }

    public void OpenHypothesis()
    {
        gameObject.SetActive(true);

        panel.Build(
            "El culpable fue {0} usando {1} en {2}",
            BuildSlotOptions()
        );
    }

    public void CloseHypothesis()
    {
        gameObject.SetActive(false);
    }

    // =========================
    // SLOT OPTIONS PER TYPE
    // =========================
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
                {
                    if (kvp.Value.IsEmpty) continue;
                    options.Add(kvp.Value.item.name);
                }
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

    // =========================
    // BUILD ALL SLOT OPTIONS
    // =========================
    private List<List<string>> BuildSlotOptions()
    {
        var slots = new List<List<string>>();

        for (int i = 0; i < slotDefinitions.Count; i++)
        {
            slots.Add(BuildOptions(slotDefinitions[i].type));
        }

        return slots;
    }
    
    public void OnDropdownChanged(int index, string value)
    {
        Debug.Log($"Slot {index}: {value}");
    }
}