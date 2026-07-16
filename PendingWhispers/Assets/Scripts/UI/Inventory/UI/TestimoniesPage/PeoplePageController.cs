using System.Collections.Generic;
using Inventory.UI;
using UnityEngine;
using UnityEngine.UI;

public class PeoplePageController : JournalPageController
{
    [SerializeField] private GameObject listPanel;
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private UIInventoryDescription descriptionPanel;

    [Header("UI")]
    [SerializeField] private Transform content;
    [SerializeField] private GameObject entryPrefab;

    private readonly List<PeopleEntryUI> slots = new();

    public override void Refresh()
    {
        Debug.Log("Refresh People");

        if (PeopleJournalSystem.Instance == null)
            return;

        var entries = PeopleJournalSystem.Instance.GetEntries();

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

            if (i < entries.Count)
                slots[i].SetData(entries[i]);
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content.GetComponent<RectTransform>());
    }

    private void HandleClick(PersonJournalEntry entry)
    {
        foreach (var slot in slots)
        {
            if (slot.GetData() == entry)
                slot.Select();
            else
                slot.Deselect();
        }

        ShowDetail(entry);
    }

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

    public override void Show()
    {
        base.Show();
        ShowList();
    }
}