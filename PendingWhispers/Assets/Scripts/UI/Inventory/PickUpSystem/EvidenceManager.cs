using System.Collections.Generic;
using UnityEngine;
using Inventory.Model;

public class EvidenceManager : MonoBehaviour
{
    public static EvidenceManager Instance;

    [Header("Config")]
    public List<ItemSO> requiredItems;
    public string completedFlag = "all_evidence_collected";

    private bool completed = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[Evidence] Inicializado");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void CheckCompletion()
    {
        Debug.Log("[Evidence] CheckCompletion llamado");

        if (completed)
        {
            Debug.Log("[Evidence] Ya completado");
            return;
        }

        if (InventoryRuntime.Instance == null)
        {
            Debug.LogError("[Evidence] InventoryRuntime NULL");
            return;
        }

        var inventory = InventoryRuntime.Instance.GetInventory();
        var items = inventory.GetCurrentInventoryState();

        Debug.Log("[Evidence] Items actuales: " + items.Count);

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
            {
                Debug.Log("[Evidence] Falta: " + required.name);
                return;
            }
        }

        completed = true;

        Debug.Log("[Evidence] TODAS LAS PRUEBAS COMPLETADAS");

        GameState.Instance.AddFlag(completedFlag);
    }
}