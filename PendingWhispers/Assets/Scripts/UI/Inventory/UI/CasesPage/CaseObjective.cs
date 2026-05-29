using UnityEngine;

[System.Serializable]
public class CaseObjective
{
    [TextArea]
    public string objectiveText;

    public FlagSO completedFlag;
}