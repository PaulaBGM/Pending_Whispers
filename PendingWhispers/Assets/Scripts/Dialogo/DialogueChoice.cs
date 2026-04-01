using System;
using System.Collections.Generic;

[Serializable]
public class DialogueChoice
{
    public string text;

    public string nextNodeID;

    public List<string> addFlags;
    public List<string> requiredFlags;
}