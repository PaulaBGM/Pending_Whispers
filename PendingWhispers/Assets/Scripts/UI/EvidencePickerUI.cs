using System;
using System.Collections.Generic;
using UnityEngine;
using Inventory.Model;
using Inventory.UI;

public class EvidencePickerUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Transform content;
    [SerializeField] private ClueEntryUI slotPrefab;

    private readonly List<ClueEntryUI> pool = new();
    private readonly List<ItemSO> boundItems = new();

    public event Action<ItemSO> OnItemSelected;
    public bool IsShowing => root != null && root.activeSelf;

    private void Awake()
    {
        if (root != null)
            root.SetActive(false);
    }

    public void Show()
    {
        Populate();
        if (root != null)
            root.SetActive(true);
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }

    private void Populate()
    {
        var inventory = InventoryRuntime.Instance?.GetInventory();
        if (inventory == null)
            return;

        boundItems.Clear();
        foreach (var kvp in inventory.GetCurrentInventoryState())
        {
            ItemSO item = kvp.Value.item;
            if (item == null)
                continue;
            if (item.ItemType != ItemType.Clue && item.ItemType != ItemType.Testimony)
                continue;
            boundItems.Add(item);
        }

        EnsureSlots(boundItems.Count);

        for (int i = 0; i < pool.Count; i++)
        {
            if (i < boundItems.Count)
                pool[i].SetData(boundItems[i].ItemImage, 1);
            else
                pool[i].ResetData();
        }
    }

    private void EnsureSlots(int count)
    {
        while (pool.Count < count)
        {
            var slot = Instantiate(slotPrefab, content);
            slot.OnItemClicked += HandleSlotClicked;
            pool.Add(slot);
        }
    }

    private void HandleSlotClicked(ClueEntryUI slot)
    {
        int index = pool.IndexOf(slot);
        if (index < 0 || index >= boundItems.Count)
            return;

        OnItemSelected?.Invoke(boundItems[index]);
    }
}
