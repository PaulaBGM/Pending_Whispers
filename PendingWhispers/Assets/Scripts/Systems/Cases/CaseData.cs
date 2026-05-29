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

    [Header("Siguiente caso")]
    public CaseData nextCase;

    [Header("Finales")]
    public List<CaseOutcome> outcomes;
}