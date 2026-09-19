using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueNode
{
    public string id;

    public string speakerID;

    [TextArea(3, 6)]
    public string text;

    public string nextNodeID;
    public List<DialogueChoice> choices;
    public List<FlagSO> requiredFlags;
    public List<FlagSO> onEnterFlags;
    public List<GameEventSO> onEnterEvents;
    public bool isImportantLine;

    [Header("Presentación de pruebas")]
    public bool allowsEvidencePresentation;
    public List<DialogueEvidenceOption> evidenceOptions;
    [Tooltip("Nodo al que saltar si el ítem presentado no coincide con ninguna opción. Si se deja vacío, el jugador se queda en este nodo y puede volver a intentarlo.")]
    public string wrongEvidenceNodeID;
    [Tooltip("Mensaje de feedback (UIFeedbackManager) cuando el ítem presentado no es correcto.")]
    public string wrongEvidenceFeedback = "Eso no parece relevante ahora mismo.";

    [Header("Expression")]
    public DialogueExpression expression = DialogueExpression.Neutral;
}