using System.Collections.Generic;

[System.Serializable]
public class CaseOutcome
{
    public string outcomeID;

    public List<FlagSO> requiredFlags;
    public List<FlagSO> resultingFlags;

    public string feedbackText;
}