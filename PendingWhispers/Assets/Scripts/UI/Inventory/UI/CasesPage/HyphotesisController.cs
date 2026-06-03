using UnityEngine;
using System.Collections.Generic;
using Inventory.Model;

public class HypothesisController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private HypothesisPanelUI panel;

    [Header("Flags")]
    [SerializeField] private FlagSO hypothesisReadyFlag;
    [SerializeField] private FlagSO correctHypothesisFlag;
    [SerializeField] private FlagSO wrongHypothesisFlag;

    private HypothesisData hypothesisData;

    // NUNCA ser� null
    private string[] currentHypothesis = System.Array.Empty<string>();

    private void Awake()
    {
        if (panel != null)
            panel.SetCallback(OnDropdownChanged);
    }

    public void OpenHypothesis()
    {
        var currentCase = CaseManager.Instance?.GetCurrentCaseData();

        if (currentCase == null)
        {
            Debug.LogError("[Hypothesis] No active CaseData");
            return;
        }

        if (currentCase.hypothesis == null)
        {
            Debug.LogError($"[Hypothesis] Case '{currentCase.caseTitle}' has no HypothesisData assigned");
            return;
        }

        hypothesisData = currentCase.hypothesis;

        int slotCount = hypothesisData.slots != null
            ? hypothesisData.slots.Count
            : 0;

        currentHypothesis = new string[slotCount];

        gameObject.SetActive(true);

        if (panel != null)
        {
            panel.Build(
                hypothesisData.textParts,
                BuildSlotOptions()
            );
        }
    }

    public void CloseHypothesis()
    {
        gameObject.SetActive(false);
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
        Debug.Log("CONFIRM HYPOTHESIS CALLED");
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

        UIFeedbackManager.Instance?.ShowMessage(
            "Hypothesis recorded."
        );
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
            Debug.LogError(
                $"[Hypothesis] Slot mismatch. Player={currentHypothesis.Length} Correct={hypothesisData.correctAnswers.Count}"
            );
            return false;
        }

        for (int i = 0; i < currentHypothesis.Length; i++)
        {
            string playerAnswer =
                currentHypothesis[i]?.Trim().ToLowerInvariant() ?? "";

            string correctAnswer = hypothesisData.correctAnswers[i]?.Trim().ToLowerInvariant() ?? "";

            Debug.Log($"Slot {i} | Player=[{playerAnswer}] | Correct=[{correctAnswer}]");

            if (playerAnswer != correctAnswer)
            {
                Debug.Log($"FAILED SLOT {i}");
                return false;
            }
        }

        return true;
    }

    private List<List<string>> BuildSlotOptions()
    {
        var slots = new List<List<string>>();

        if (hypothesisData == null || hypothesisData.slots == null)
            return slots;

        foreach (var slot in hypothesisData.slots)
        {
            slots.Add(BuildOptions(slot.type));
        }

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

                if (InventoryRuntime.Instance != null)
                {
                    var inventory =
                        InventoryRuntime.Instance
                            .GetInventory()
                            .GetCurrentInventoryState();

                    foreach (var kvp in inventory)
                    {
                        if (!kvp.Value.IsEmpty &&
                            kvp.Value.item != null)
                        {
                            options.Add(kvp.Value.item.NameHypothesis);
                        }
                    }
                }

                break;

            case HypothesisSlotType.Clue:

                if (InventoryRuntime.Instance != null)
                {
                    var items =
                        InventoryRuntime.Instance
                            .GetInventory()
                            .GetItemsByType(ItemType.Clue);

                    foreach (var item in items)
                    {
                        if (item.item != null)
                            options.Add(item.item.name);
                    }
                }

                break;
        }

        return options;
    }

    public string GetHypothesisText()
    {
        if (hypothesisData == null)
            return string.Empty;

        if (hypothesisData.textParts == null)
            return string.Empty;

        string result = string.Empty;

        int slotCount = hypothesisData.slots != null
            ? hypothesisData.slots.Count
            : 0;

        for (int i = 0; i < slotCount; i++)
        {
            if (i < hypothesisData.textParts.Count)
                result += hypothesisData.textParts[i];

            if (i < currentHypothesis.Length &&
                !string.IsNullOrEmpty(currentHypothesis[i]))
            {
                result += currentHypothesis[i];
            }
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