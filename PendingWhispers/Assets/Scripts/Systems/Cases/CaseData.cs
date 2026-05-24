using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Case/Case Data")]
public class CaseData : ScriptableObject
{
    [Header("ID")]
    public string caseID;

    [Header("Activación")]
    public FlagSO unlockFlag;      // requisito para que aparezca
    public FlagSO startedFlag;     // cuando empieza
    public FlagSO completedFlag;   // cuando termina

    [Header("Progreso")]
    public List<FlagSO> requiredClues;

    [Header("Mapa")]
    public string unlockedNodeID;

    [Header("Siguiente caso")]
    public CaseData nextCase;

    [Header("Finales")]
    public List<CaseOutcome> outcomes;
}