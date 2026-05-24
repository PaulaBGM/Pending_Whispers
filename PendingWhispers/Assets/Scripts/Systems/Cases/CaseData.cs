using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Case/Case Data")]
public class CaseData : ScriptableObject
{
    [Header("ID")]
    public string caseID;

    [Header("UI")]
    public string displayName;

    [TextArea(3, 8)]
    public string description;

    [Header("Flags")]
    public FlagSO unlockFlag;

    public FlagSO startedFlag;

    public FlagSO completedFlag;

    [Header("Pistas necesarias")]
    public List<FlagSO> requiredClues;

    [Header("Mapa")]
    public string unlockedNodeID;

    [Header("Siguiente caso")]
    public CaseData nextCase;

    [Header("Finales")]
    public List<CaseOutcome> outcomes;
}