using Inventory.Model;
using System;
using UnityEngine;

[Serializable]
public struct TestimonyEntry
{
    public string Name;
    public Sprite Portrait;
    public string Dialogue;
    public TestimonyItemSO TestimonyItem;

    public TestimonyEntry(string name, Sprite portrait, string dialogue, TestimonyItemSO testimony)
    {
        Name = name;
        Portrait = portrait;
        Dialogue = dialogue;
        TestimonyItem = testimony;
    }
}
