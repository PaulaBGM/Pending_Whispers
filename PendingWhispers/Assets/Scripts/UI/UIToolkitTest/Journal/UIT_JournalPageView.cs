using System.Collections.Generic;
using Inventory.Model;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Base de las páginas del diario en UI Toolkit (sustituye a JournalPageController).
/// Una página puede ocupar varios elementos del UXML (p. ej. su parte en la hoja
/// izquierda y su parte en la derecha); todos se muestran y ocultan juntos.
/// El UIT_JournalController llama a Bind una vez y luego a Show/Hide.
/// </summary>
public abstract class UIT_JournalPageView : MonoBehaviour
{
    [Tooltip("Elementos del UXML que forman esta página. El primero es la raíz principal.")]
    [SerializeField] private string[] elementNames;

    private readonly List<VisualElement> elements = new();

    protected VisualElement PageRoot => elements.Count > 0 ? elements[0] : null;
    public bool IsBound => elements.Count > 0;
    public bool IsVisible { get; private set; }

    public void Bind(VisualElement journalRoot)
    {
        elements.Clear();
        PrepareElements(journalRoot);

        foreach (string elementName in elementNames)
        {
            var element = journalRoot.Q<VisualElement>(elementName);
            if (element != null)
                elements.Add(element);
            else
                Debug.LogError($"{name}: no se encontró '{elementName}' en el UXML.", this);
        }

        if (!IsBound)
            return;

        OnBind(journalRoot);
        SetVisible(false);
    }

    public void Show(ItemType filter)
    {
        if (!IsBound)
            return;

        SetVisible(true);
        OnShow(filter);
        Refresh();
    }

    public void Hide()
    {
        if (!IsBound || !IsVisible)
            return;

        OnHide();
        SetVisible(false);
    }

    /// <summary>
    /// Busca solo dentro de los elementos de esta página. Importante porque en el UXML
    /// hay nombres repetidos (p. ej. SV_SelectorContainer) y Q devuelve el primero.
    /// </summary>
    protected T FindInPage<T>(string elementName) where T : VisualElement
    {
        if (string.IsNullOrEmpty(elementName))
            return null;

        foreach (var element in elements)
        {
            var result = element.Q<T>(elementName);
            if (result != null)
                return result;
        }

        Debug.LogWarning($"{name}: no se encontró '{elementName}' dentro de la página.", this);
        return null;
    }

    private void SetVisible(bool visible)
    {
        IsVisible = visible;
        foreach (var element in elements)
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    /// <summary>
    /// Se llama antes de buscar los elementos de la página. Sirve para crear por código
    /// las partes que falten en el UXML (p. ej. si se ha perdido al editarlo).
    /// </summary>
    protected virtual void PrepareElements(VisualElement journalRoot) { }

    protected abstract void OnBind(VisualElement journalRoot);
    protected virtual void OnShow(ItemType filter) { }
    protected virtual void OnHide() { }
    public abstract void Refresh();
}
