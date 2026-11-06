using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Case/Case Data")]
public class CaseData : ScriptableObject
{
    [Header("UI")]
    public string caseTitle;

    [TextArea(4, 8)]
    public string caseDescription;

    public Sprite caseIcon;

    [Header("Objectives")]
    public List<CaseObjective> objectives;
    
    [Header("Hypothesis")]
    public HypothesisData hypothesis;
    
    [Header("ID")]
    public string caseID;

    [Header("Activación")]
    public FlagSO unlockFlag;
    public FlagSO startedFlag;
    public FlagSO completedFlag;

    [Header("Progreso")]
    public List<FlagSO> requiredClues;

    [Header("Mapa")]
    public string unlockedNodeID;

    [Header("Al aceptar en la pizarra (HUB)")]
    [Tooltip("Flags que se anaden al aceptar el caso: el del mapa (MapUnlocked) y el unlockFlag " +
             "del NodeData al que hay que ir, para que su boton aparezca en el mapa.")]
    public List<FlagSO> onAcceptFlags = new();

    [Header("Siguiente caso")]
    public CaseData nextCase;

    [Header("Finales")]
    public List<CaseOutcome> outcomes;

    [Header("Pizarra (HUB)")]
    public string requesterName;
    public string requesterLocation;
    [TextArea(3, 6)] public string requestSummary;   // "Descripción general de la petición"
    public int baseReputationReward = 100;            // el "+I00%" que se ve antes de aceptar
    public Color cardColor = Color.white;              // rosa/verde/beige del mockup
}