using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class JournalEntryUI<T> : MonoBehaviour, IPointerClickHandler
    where T : class
{
    [Header("Selection")]
    [SerializeField] protected Image selectedBorder;

    protected T data;

    public event Action<T> OnEntryClicked;

    protected void SetEntry(T entry)
    {
        data = entry;
        gameObject.SetActive(true);
    }

    public virtual void ResetData()
    {
        data = null;

        Deselect();

        gameObject.SetActive(false);
    }

    public virtual void Select()
    {
        if (selectedBorder != null)
            selectedBorder.enabled = true;
    }

    public virtual void Deselect()
    {
        if (selectedBorder != null)
            selectedBorder.enabled = false;
    }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        if (data != null)
            OnEntryClicked?.Invoke(data);
    }

    public T GetData()
    {
        return data;
    }
}