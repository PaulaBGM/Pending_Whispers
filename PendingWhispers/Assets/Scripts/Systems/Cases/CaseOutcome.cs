using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class CaseOutcome
{
    public string outcomeID;

    [TextArea]
    public string feedbackText;

    public List<FlagSO> requiredFlags;

    public List<FlagSO> resultingFlags;
}