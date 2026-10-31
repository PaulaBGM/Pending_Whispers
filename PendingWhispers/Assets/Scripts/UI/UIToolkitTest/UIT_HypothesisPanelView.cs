using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Vista UI Toolkit de la hipótesis (sustituye a HypothesisPanelUI, misma API: SetCallback / Build).
/// Genera las filas (texto + desplegable) dentro de VE_HyphotesisTextContainer según los huecos
/// del HypothesisData, en lugar de usar filas fijas.
/// La lógica (respuestas, comprobación, flags) sigue en UIT_HypothesisController.
/// </summary>
public class UIT_HypothesisPanelView : MonoBehaviour
{
    private const string RowClass = "hypothesis-row";
    private const string TextClass = "hypothesis-text";
    private const string DropdownClass = "hypothesis-dropdown";

    [SerializeField] private string rowsContainerName = "VE_HyphotesisTextContainer";
    [SerializeField] private string confirmButtonName = "BT_ConfirmHyphotesis";

    private VisualElement rowsContainer;
    private Action<int, string> onValueChanged;
    private Action onConfirm;

    public bool IsBound => rowsContainer != null;

    public void SetCallback(Action<int, string> callback) => onValueChanged = callback;

    public void SetConfirmCallback(Action callback) => onConfirm = callback;

    /// <summary>Lo llama UIT_CasePageView con la raíz de la página de hipótesis.</summary>
    public void Bind(VisualElement hypothesisPage)
    {
        if (hypothesisPage == null)
            return;

        rowsContainer = hypothesisPage.Q<VisualElement>(rowsContainerName);
        if (rowsContainer == null)
            Debug.LogError($"{name}: no se encontró '{rowsContainerName}'.", this);

        var confirmButton = hypothesisPage.Q<Button>(confirmButtonName);
        if (confirmButton != null)
            confirmButton.clicked += () => onConfirm?.Invoke();
        else
            Debug.LogWarning($"{name}: no se encontró '{confirmButtonName}'.", this);
    }

    /// <param name="currentValues">Respuestas ya elegidas, para no perderlas al reabrir.</param>
    public void Build(List<string> textParts, List<List<string>> slotOptions, IReadOnlyList<string> currentValues = null)
    {
        if (!IsBound)
        {
            Debug.LogWarning($"{name}: Build llamado antes de Bind.", this);
            return;
        }

        rowsContainer.Clear();

        int slotCount = slotOptions?.Count ?? 0;
        int textCount = textParts?.Count ?? 0;

        for (int i = 0; i < slotCount; i++)
        {
            var row = new VisualElement();
            row.AddToClassList(RowClass);

            if (i < textCount)
                row.Add(CreateText(textParts[i]));

            row.Add(CreateDropdown(i, slotOptions[i], GetValue(currentValues, i)));
            rowsContainer.Add(row);
        }

        // Texto que va después del último hueco.
        for (int i = slotCount; i < textCount; i++)
            rowsContainer.Add(CreateText(textParts[i]));
    }

    private Label CreateText(string text)
    {
        var label = new Label(text);
        label.AddToClassList(TextClass);
        return label;
    }

    private DropdownField CreateDropdown(int index, List<string> options, string currentValue)
    {
        // Empieza vacío a propósito: así IsHypothesisComplete() sí detecta huecos sin rellenar.
        var dropdown = new DropdownField { choices = new List<string>(options ?? new List<string>()) };
        dropdown.AddToClassList(DropdownClass);

        if (!string.IsNullOrEmpty(currentValue) && dropdown.choices.Contains(currentValue))
            dropdown.SetValueWithoutNotify(currentValue);

        dropdown.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(index, evt.newValue));
        return dropdown;
    }

    private static string GetValue(IReadOnlyList<string> values, int index)
    {
        return values != null && index < values.Count ? values[index] : null;
    }
}
