using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class JournalEntryUI<T> : MonoBehaviour, IPointerClickHandler
    where T : class, IJournalEntry
{
    [SerializeField] protected Image icon;
    [SerializeField] protected TMP_Text title;
    [SerializeField] protected GameObject selectedBorder;

    protected T data;

    public event Action<T> OnEntryClicked;

    public virtual void SetData(T entry)
    {
        data = entry;

        if (entry == null)
        {
            ResetData();
            return;
        }

        if (icon != null)
        {
            icon.sprite = entry.Icon;
            icon.gameObject.SetActive(true);
        }

        if (title != null)
            title.text = entry.Title;

        gameObject.SetActive(true);

        RefreshExtraUI(entry);
    }

    protected virtual void RefreshExtraUI(T entry)
    {
    }

    public virtual void ResetData()
    {
        data = null;

        if (icon != null)
        {
            icon.sprite = null;
            icon.gameObject.SetActive(false);
        }

        if (title != null)
            title.text = "";

        gameObject.SetActive(false);
    }

    public void Select()
    {
        if (selectedBorder != null)
            selectedBorder.SetActive(true);
    }

    public void Deselect()
    {
        if (selectedBorder != null)
            selectedBorder.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (data != null)
            OnEntryClicked?.Invoke(data);
    }

    public T GetData()
    {
        return data;
    }
}