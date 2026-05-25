using System.Collections;
using System.Collections.Generic;
using Inventory.UI;
using UnityEngine.UI;
using UnityEngine;

public class PeoplePageController : MonoBehaviour
{
    [SerializeField] private GameObject listPanel;
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private UIInventoryDescription descriptionPanel;

    [Header("UI")]
    [SerializeField] private Transform content;
    [SerializeField] private GameObject entryPrefab;

    private List<PersonJournalEntry> cachedEntries = new();
    private List<PeopleEntryUI> slots = new();
    private List<int> filteredIndices = new();

    private PersonJournalEntry selectedEntry;
    private PeopleEntryUI selectedSlot;

    private void Start()
    {
        RefreshUI();
    }

    // =========================
    // REFRESH (tipo InventoryController)
    // =========================
    public void RefreshUI()
    {
        var entries = PeopleJournalSystem.Instance.GetEntries();

        filteredIndices.Clear();

        while (slots.Count < entries.Count)
        {
            var obj = Instantiate(entryPrefab, content);
            var ui = obj.GetComponent<PeopleEntryUI>();

            ui.OnEntryClicked += HandleClick;
            slots.Add(ui);
        }

        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].ResetData();
            slots[i].Deselect();
        }

        int uiIndex = 0;

        for (int i = 0; i < entries.Count; i++)
        {
            filteredIndices.Add(i);
            slots[uiIndex].SetData(entries[i]);
            uiIndex++;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content.GetComponent<RectTransform>());
    }

    private void HandleClick(PersonJournalEntry entry)
    {
        for (int i = 0; i < cachedEntries.Count; i++)
        {
            bool isSelected = cachedEntries[i] == entry;

            if (isSelected)
            {
                slots[i].Select();
                selectedSlot = slots[i];
                selectedEntry = entry;
            }
            else
            {
                slots[i].Deselect();
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
        if (entry == null || descriptionPanel == null)
            return;

        detailPanel.SetActive(true);

        descriptionPanel.gameObject.SetActive(true);

        descriptionPanel.SetDescription(
            entry.portrait,
            entry.personName,
            entry.fullDialogue
        );
    }
}