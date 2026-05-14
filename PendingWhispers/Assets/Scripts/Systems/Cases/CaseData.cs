using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Case/Case Data")]
public class CaseData : ScriptableObject
{
    public string caseID;

    [Header("Progreso")]
    public List<FlagSO> requiredClues;

    [Header("Finales")]
    public List<CaseOutcome> outcomes;
}