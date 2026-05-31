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
    private string[] currentHypothesis;

    private void Awake()
    {
        if (panel != null)
            panel.SetCallback(OnDropdownChanged);
    }

    public void OpenHypothesis()
    {
        var currentCase = CaseManager.Instance.GetCurrentCaseData();

        if (currentCase == null)
        {
            Debug.LogError("No active CaseData");
            return;
        }

        if (currentCase.hypothesis == null)
        {
            Debug.LogError($"Case '{currentCase.caseTitle}' has no HypothesisData assigned");
            return;
        }

        gameObject.SetActive(true);

        hypothesisData = currentCase.hypothesis;

        if (currentHypothesis == null || currentHypothesis.Length != hypothesisData.slots.Count)
            currentHypothesis = new string[hypothesisData.slots.Count];

        panel.Build(
            hypothesisData.textParts,
            BuildSlotOptions()
        );
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

        currentHypothesis[index] = value;
    }

    public void ConfirmHypothesis()
    {
        if (!IsHypothesisComplete())
        {
            UIFeedbackManager.Instance.ShowMessage(
                "Complete the hypothesis before continuing."
            );
            return;
        }

        if (hypothesisReadyFlag != null)
        {
            GameProgress.Instance.AddFlag(hypothesisReadyFlag);
        }

        if (IsCorrect())
        {
            if (correctHypothesisFlag != null)
            {
                GameProgress.Instance.AddFlag(correctHypothesisFlag);
            }
        }
        else
        {
            if (wrongHypothesisFlag != null)
            {
                GameProgress.Instance.AddFlag(wrongHypothesisFlag);
            }
        }

        UIFeedbackManager.Instance.ShowMessage(
            "Hypothesis recorded."
        );
    }

    public bool IsHypothesisComplete()
    {
        if (currentHypothesis == null)
            return false;

        foreach (string answer in currentHypothesis)
        {
            if (string.IsNullOrEmpty(answer))
                return false;
        }

        return true;
    }

    public bool IsCorrect()
    {
        if (hypothesisData == null)
            return false;

        if (currentHypothesis == null)
            return false;

        if (currentHypothesis.Length != hypothesisData.correctAnswers.Count)
            return false;

        for (int i = 0; i < currentHypothesis.Length; i++)
        {
            Debug.Log(
                $"Slot {i} | Player=[{currentHypothesis[i]}] | Correct=[{hypothesisData.correctAnswers[i]}]"
            );

            if (
                currentHypothesis[i].Trim().ToLowerInvariant()
                !=
                hypothesisData.correctAnswers[i].Trim().ToLowerInvariant()
            )
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

        if (hypothesisData == null)
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
                        options.Add(person.personName);
                    }
                }

                break;

            case HypothesisSlotType.Item:

                if (InventoryRuntime.Instance != null)
                {
                    var inventory = InventoryRuntime.Instance.GetInventory().GetCurrentInventoryState();

                    foreach (var kvp in inventory)
                    {
                        if (!kvp.Value.IsEmpty)
                            options.Add(kvp.Value.item.NameHypothesis);
                    }
                }

                break;

            case HypothesisSlotType.Clue:

                if (InventoryRuntime.Instance != null)
                {
                    var items = InventoryRuntime.Instance.GetInventory().GetItemsByType(ItemType.Clue);

                    foreach (var item in items)
                    {
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
            return "";

        if (currentHypothesis == null)
            return "";

        string result = "";

        for (int i = 0; i < hypothesisData.slots.Count; i++)
        {
            result += hypothesisData.textParts[i];

            if (i < currentHypothesis.Length && !string.IsNullOrEmpty(currentHypothesis[i]))
                result += currentHypothesis[i];
        }

        result += hypothesisData.textParts[^1];

        return result;
    }

    public string[] GetCurrentHypothesis()
    {
        return currentHypothesis;
    }
}