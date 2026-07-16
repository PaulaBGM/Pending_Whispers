using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class JournalEntryUI<T> : MonoBehaviour, IPointerClickHandler
{
    [Header("Selection")]
    [SerializeField] protected Image selectedBorder;

    protected T data;

    public event Action<T> OnEntryClicked;

    /// <summary>
    /// Guarda el dato asociado al slot y lo activa.
    /// </summary>
    protected void SetEntry(T entry)
    {
        data = entry;
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Limpia el contenido del slot.
    /// Cada clase hija se encarga de limpiar su propia UI.
    /// </summary>
    public virtual void ResetData()
    {
        data = default;

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

    public bool HasData()
    {
        return data != null;
    }
}