using System.Collections.Generic;

[System.Serializable]
public class CaseState
{
    public string caseID;

    public HashSet<FlagSO> flags = new();
    public Dictionary<string, int> variables = new();

    public void AddFlag(FlagSO flag)
    {
        flags.Add(flag);
    }

    public bool HasFlag(FlagSO flag)
    {
        return flags.Contains(flag);
    }

    public bool HasAll(List<FlagSO> required)
    {
        if (required == null || required.Count == 0) return true;

        foreach (var r in required)
            if (!flags.Contains(r)) return false;

        return true;
    }
}