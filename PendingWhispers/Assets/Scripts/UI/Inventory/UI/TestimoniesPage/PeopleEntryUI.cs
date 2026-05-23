using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PeopleEntryUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image portrait;
    [SerializeField] private Image borderImage;

    public event Action<PersonJournalEntry> OnEntryClicked;

    private PersonJournalEntry data;

    public bool empty = true;

    private void Awake()
    {
        ResetData();
        Deselect();
    }

    // =========================
    // RESET (igual que UIInventoryItem)
    // =========================
    public void ResetData()
    {
        if (portrait == null) return;

        portrait.sprite = null;
        portrait.gameObject.SetActive(false);

        data = null;
        empty = true;
    }

    // =========================
    // SET DATA (igual que UIInventoryItem.SetData)
    // =========================
    public void SetData(PersonJournalEntry entry)
    {
        if (portrait == null || entry == null) return;

        data = entry;

        portrait.gameObject.SetActive(true);
        portrait.sprite = entry.portrait;

        empty = false;
    }

    // =========================
    // SELECTION (igual que inventory)
    // =========================
    public void Select()
    {
        if (borderImage != null)
            borderImage.enabled = true;
    }

    public void Deselect()
    {
        if (borderImage != null)
            borderImage.enabled = false;
    }

    // =========================
    // CLICK
    // =========================
    public void OnPointerClick(PointerEventData eventData)
    {
        if (empty || data == null) return;

        OnEntryClicked?.Invoke(data);
    }

    public PersonJournalEntry GetData()
    {
        return data;
    }
}