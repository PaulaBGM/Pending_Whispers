using System;
using UnityEngine;

[Serializable]
public struct TestimonyEntry
{
    public string Name;
    public Sprite Portrait;
    public string Dialogue;

    public TestimonyEntry(string name, Sprite portrait, string dialogue)
    {
        Name = name;
        Portrait = portrait;
        Dialogue = dialogue;
    }
}
