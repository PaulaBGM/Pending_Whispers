using System.Collections.Generic;
using UnityEngine;
using Inventory.Model;

public class EvidenceManager : MonoBehaviour
{
    public static EvidenceManager Instance;

    public List<ItemSO> requiredItems;
    public FlagSO completedFlag;

    private bool completed = false;

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        var inventory = InventoryRuntime.Instance.GetInventory();
        inventory.OnInventoryChanged += CheckCompletion;
    }

    public void CheckCompletion()
    {
        if (completed) return;

        var inventory = InventoryRuntime.Instance.GetInventory();
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

            if (!found) return;
        }

        completed = true;

        Debug.Log("[Evidence] COMPLETADO");

        GameProgress.Instance.AddFlag(completedFlag);
    }
}