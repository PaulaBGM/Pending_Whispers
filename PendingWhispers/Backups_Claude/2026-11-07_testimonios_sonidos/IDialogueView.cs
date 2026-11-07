using System;
using System.Collections.Generic;
using Inventory.Model;
using UnityEngine;

/// <summary>
/// Lo que DialogueManager necesita de una vista de diálogo.
/// La cumplen DialogueUI (uGUI, respaldo) y UIT_DialogueUI (UI Toolkit),
/// así el manager funciona con cualquiera de las dos sin duplicarlo.
/// </summary>
public interface IDialogueView
{
    event Action<ItemSO> OnEvidenceItemSelected;

    void ShowLine(DialogueCharacter character, string speaker, string text, Sprite expressionSprite);
    void ShowChoices(List<DialogueChoice> choices);
    void ShowDialogueSelector(List<DialogueCondition> topics,
                              Func<DialogueCondition, bool> isTopicSeen,
                              Action<DialogueCondition> onTopicSelected);
    void ClearChoices();
    void Hide();

    void SetEvidenceButtonVisible(bool visible);
    void OpenEvidencePicker();
}
