using System.Collections.Generic;

[System.Serializable]
public class DialogueChoice
{
    public string text;

    public string nextNodeID;

    public List<FlagSO> addFlags;
    public List<FlagSO> requiredFlags;

    public bool endsDialogue;
}