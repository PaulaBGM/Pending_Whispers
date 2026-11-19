using System;
using Inventory.Model;

/// <summary>
/// Petición para abrir el diario y elegir algo. Quien abre el diario decide
/// qué se puede elegir y qué pasa al elegirlo; el diario no sabe nada del diálogo.
/// </summary>
public sealed class JournalSelectionRequest
{
    public string ActionLabel = "Present";
    public Func<ItemSO, bool> CanSelect;
    public Action<ItemSO> OnSelected;
    public Action OnCancelled;
}