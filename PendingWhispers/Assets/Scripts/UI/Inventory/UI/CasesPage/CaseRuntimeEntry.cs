using System.Collections.Generic;
using Inventory.Model;

[System.Serializable]
public class CaseRuntimeEntry
{
    public CaseItemSO data;
    public List<string> discoveredClues = new();

    public int GetProgress()
    {
        return discoveredClues.Count;
    }

    public string GetProgressText()
    {
        return $"{discoveredClues.Count}/{data.TotalClues}";
    }
}