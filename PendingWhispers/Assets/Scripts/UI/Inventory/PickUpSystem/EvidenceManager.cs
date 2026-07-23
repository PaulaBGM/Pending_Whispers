using System.Collections.Generic;
using UnityEngine;
using Inventory.Model;

public class EvidenceManager : BaseSingleton<EvidenceManager>
{
    [SerializeField] private InventorySO inventory;

    [SerializeField] private List<ItemSO> requiredItems;
    [SerializeField] private FlagSO completedFlag;

    private bool completed;

    protected override void Awake()
    {
        base.Awake();
    }
    protected override void OnDestroy()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= CheckCompletion;
        base.OnDestroy();
    }
    private void Start()
    {
        if (inventory == null)
        {
            Debug.LogError("[EvidenceManager] InventorySO no asignado");
            return;
        }

        inventory.OnInventoryChanged += CheckCompletion;
    }

    public void CheckCompletion()
    {
        if (completed || inventory == null)
            return;

        var items = inventory.GetCurrentInventoryState();

        foreach (var required in requiredItems)
        {
            bool found = false;

            foreach (var kvp in items)
            {
                if (kvp.Value.item == required)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
                return;
        }

        completed = true;
        GameProgress.Instance.AddFlag(completedFlag);
    }
}