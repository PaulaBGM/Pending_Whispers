using System;
using System.Collections.Generic;

[Serializable]
public class DialogueLine
{
    public string speakerName;
    [UnityEngine.TextArea] public string text;

    public List<DialogueChoice> choices;
}