using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CaseOutcome
{
    public string outcomeID;

    public List<FlagSO> requiredFlags;
    public List<FlagSO> resultingFlags;

    public string feedbackText;

    [Header("Reputation")]
    public int reputationReward;
}