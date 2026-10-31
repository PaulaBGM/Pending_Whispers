using UnityEngine;
using System.Collections.Generic;
using Inventory.Model;

public class UIT_HypothesisController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private UIT_HypothesisPanelView panel;

    [Header("Flags")]
    [SerializeField] private FlagSO hypothesisReadyFlag;
    [SerializeField] private FlagSO correctHypothesisFlag;
    [SerializeField] private FlagSO wrongHypothesisFlag;

    private HypothesisData hypothesisData;

    // NUNCA sera null
    private string[] currentHypothesis = System.Array.Empty<string>();

    private void Awake()
    {
        if (panel != null)
        {
            panel.SetCallback(OnDropdownChanged);
            panel.SetConfirmCallback(ConfirmHypothesis);
        }
    }

    /// <summary>Abre la hipótesis del caso activo (compatibilidad con llamadas antiguas).</summary>
    public void OpenHypothesis()
    {
        TryOpenHypothesis(CaseManager.Instance?.GetCurrentCaseData());
    }

    /// <summary>Abre la hipótesis de un caso concreto. Conserva las respuestas si es la misma hipótesis.</summary>
    public bool TryOpenHypothesis(CaseData caseData)
    {
        if (caseData == null)
        {
            Debug.LogError("[Hypothesis] No active CaseData");
            return false;
        }

        if (caseData.hypothesis == null)
        {
            Debug.LogError($"[Hypothesis] Case '{caseData.caseTitle}' has no HypothesisData assigned");
            return false;
        }

        int slotCount = caseData.hypothesis.slots != null ? caseData.hypothesis.slots.Count : 0;
        bool sameHypothesis = hypothesisData == caseData.hypothesis && currentHypothesis.Length == slotCount;

        hypothesisData = caseData.hypothesis;

        // Antes se reiniciaba siempre: al reabrir el diario se perdía la hipótesis ya confirmada
        // y UIT_HypothesisDialogueResolver la evaluaba vacía.
        if (!sameHypothesis)
            currentHypothesis = new string[slotCount];

        if (panel != null)
            panel.Build(hypothesisData.textParts, BuildSlotOptions(), currentHypothesis);

        return true;
    }

    /// <summary>
    /// Se mantiene por compatibilidad. Ya no desactiva el GameObject: la visibilidad
    /// la gestiona UIT_CasePageView, y desactivar el objeto del UIDocument reconstruiría el árbol.
    /// </summary>
    public void CloseHypothesis()
    {
    }

    private void OnDropdownChanged(int index, string value)
    {
        if (currentHypothesis == null)
            return;

        if (index < 0 || index >= currentHypothesis.Length)
            return;

        currentHypothesis[index] = value ?? string.Empty;
    }

    public void ConfirmHypothesis()
    {
        if (!IsHypothesisComplete())
        {
            UIFeedbackManager.Instance?.ShowMessage("Complete the hypothesis before continuing.");
            return;
        }

        if (hypothesisReadyFlag != null)
            GameProgress.Instance?.AddFlag(hypothesisReadyFlag);

        if (IsCorrect())
        {
            if (correctHypothesisFlag != null)
                GameProgress.Instance?.AddFlag(correctHypothesisFlag);
        }
        else
        {
            if (wrongHypothesisFlag != null)
                GameProgress.Instance?.AddFlag(wrongHypothesisFlag);
        }
        UIFeedbackManager.Instance?.ShowMessage("Hypothesis recorded.");
    }

    public bool IsHypothesisComplete()
    {
        if (currentHypothesis.Length == 0)
            return false;

        foreach (string answer in currentHypothesis)
        {
            if (string.IsNullOrWhiteSpace(answer))
                return false;
        }

        return true;
    }

    public bool IsCorrect()
    {
        if (hypothesisData == null)
        {
            Debug.LogError("[Hypothesis] hypothesisData is NULL");
            return false;
        }

        if (hypothesisData.correctAnswers == null)
        {
            Debug.LogError("[Hypothesis] correctAnswers is NULL");
            return false;
        }

        if (currentHypothesis.Length != hypothesisData.correctAnswers.Count)
        {
            Debug.LogError($"[Hypothesis] Slot mismatch. Player={currentHypothesis.Length} Correct={hypothesisData.correctAnswers.Count}");
            return false;
        }

        for (int i = 0; i < currentHypothesis.Length; i++)
        {
            string playerAnswer = currentHypothesis[i]?.Trim().ToLowerInvariant() ?? "";
            string correctAnswer = hypothesisData.correctAnswers[i]?.Trim().ToLowerInvariant() ?? "";
            if (playerAnswer != correctAnswer)
                return false;
        }

        return true;
    }

    private List<List<string>> BuildSlotOptions()
    {
        var slots = new List<List<string>>();

        if (hypothesisData == null || hypothesisData.slots == null)
            return slots;

        foreach (var slot in hypothesisData.slots)
            slots.Add(BuildOptions(slot.type));

        return slots;
    }

    private List<string> BuildOptions(HypothesisSlotType type)
    {
        var options = new List<string>();

        switch (type)
        {
            case HypothesisSlotType.Person:
                options.Add("Nobody");
                if (PeopleJournalSystem.Instance != null)
                {
                    foreach (var person in PeopleJournalSystem.Instance.GetEntries())
                    {
                        if (!string.IsNullOrEmpty(person.personName))
                            options.Add(person.personName);
                    }
                }
                break;

            case HypothesisSlotType.Item:
                // Antes listaba todo el inventario (pistas incluidas). Ahora solo objetos.
                if (InventoryRuntime.Instance != null)
                {
                    var items = InventoryRuntime.Instance.GetInventory().GetItemsByType(ItemType.Item);
                    foreach (var item in items)
                        if (item.item != null)
                            options.Add(item.item.NameHypothesis);
                }
                break;

            case HypothesisSlotType.Clue:
                if (InventoryRuntime.Instance != null)
                {
                    var items = InventoryRuntime.Instance.GetInventory().GetItemsByType(ItemType.Clue);
                    foreach (var item in items)
                        if (item.item != null)
                            options.Add(item.item.Name);
                }
                break;
        }

        return options;
    }

    public string GetHypothesisText()
    {
        if (hypothesisData == null || hypothesisData.textParts == null)
            return string.Empty;

        string result = string.Empty;
        int slotCount = hypothesisData.slots != null ? hypothesisData.slots.Count : 0;

        for (int i = 0; i < slotCount; i++)
        {
            if (i < hypothesisData.textParts.Count)
                result += hypothesisData.textParts[i];

            if (i < currentHypothesis.Length && !string.IsNullOrEmpty(currentHypothesis[i]))
                result += currentHypothesis[i];
        }

        if (hypothesisData.textParts.Count > 0)
            result += hypothesisData.textParts[^1];

        return result;
    }

    public string[] GetCurrentHypothesis()
    {
        return currentHypothesis ?? System.Array.Empty<string>();
    }
}
