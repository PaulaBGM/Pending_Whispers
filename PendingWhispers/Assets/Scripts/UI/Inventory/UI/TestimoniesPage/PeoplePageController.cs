using System.Collections;
using System.Collections.Generic;
using Inventory.UI;
using UnityEngine;

public class PeoplePageController : MonoBehaviour
{
    [SerializeField] private GameObject listPanel;
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private UIInventoryDescription descriptionPanel;

    [Header("UI")]
    [SerializeField] private Transform content;
    [SerializeField] private GameObject entryPrefab;

    private List<PeopleEntryUI> slots = new();
    private List<int> filteredIndices = new();

    private PersonJournalEntry selectedEntry;
    private PeopleEntryUI selectedSlot;

    private IEnumerator Start()
    {
        yield return new WaitUntil(() =>
            PeopleJournalSystem.Instance != null &&
            PeopleJournalSystem.Instance.GetEntries().Count > 0
        );

        RefreshUI();
    }

    // =========================
    // REFRESH (tipo InventoryController)
    // =========================
    public void RefreshUI()
    {
        var entries = PeopleJournalSystem.Instance.GetEntries();

        // reset filtrado
        filteredIndices.Clear();

        // crear slots si faltan
        while (slots.Count < entries.Count)
        {
            var obj = Instantiate(entryPrefab, content);
            var ui = obj.GetComponent<PeopleEntryUI>();

            ui.OnEntryClicked += HandleClick;

            slots.Add(ui);
        }

        // reset total (igual que inventoryUI.ResetAllItems)
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].ResetData();
            slots[i].Deselect();
        }

        // update filtrado + UI
        int uiIndex = 0;

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];

            filteredIndices.Add(i);

            slots[uiIndex].SetData(entry);

            uiIndex++;
        }
    }

    // =========================
    // CLICK HANDLER (tipo InventoryController)
    // =========================
    private void HandleClick(PersonJournalEntry entry)
    {
        var entries = PeopleJournalSystem.Instance.GetEntries();

        for (int uiIndex = 0; uiIndex < slots.Count; uiIndex++)
        {
            if (uiIndex >= filteredIndices.Count) continue;

            var realIndex = filteredIndices[uiIndex];
            var data = entries[realIndex];

            bool isSelected = data == entry;

            if (isSelected)
            {
                slots[uiIndex].Select();
                selectedSlot = slots[uiIndex];
                selectedEntry = entry;
            }
            else
            {
                slots[uiIndex].Deselect();
            }
        }

        ShowDetail(entry);
    }

    // =========================
    // UI STATE
    // =========================
    public void ShowList()
    {
        listPanel.SetActive(true);
        detailPanel.SetActive(false);
    }

    public void ShowDetail()
    {
        detailPanel.SetActive(true);
    }

    public void ShowDetail(PersonJournalEntry entry)
    {
        if (entry == null) return;

        descriptionPanel.SetDescription(
            entry.portrait,
            entry.personName,
            entry.fullDialogue
        );

        ShowDetail();
    }
}