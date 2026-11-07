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
    /// <param name="title">Nombre del NPC (en lugar de "Talk"). Vacío = texto por defecto de la vista.</param>
    void ShowDialogueSelector(string title,
                              List<DialogueCondition> topics,
                              Func<DialogueCondition, bool> isTopicSeen,
                              Action<DialogueCondition> onTopicSelected);
    void ClearChoices();
    void Hide();

    void SetEvidenceButtonVisible(bool visible);
    void OpenEvidencePicker();
}
