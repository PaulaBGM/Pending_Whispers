using System;
using System.Collections.Generic;
using UnityEngine;

public class CaseJournalSystem : BaseSingleton<CaseJournalSystem>
{

    private Dictionary<string, CaseRuntime> cases = new();

    public event Action OnCasesChanged;

    public bool TryAddCase(CaseRuntime runtime)
    {
        if (runtime == null || runtime.data == null)
            return false;

        string caseID = runtime.data.caseID;

        if (string.IsNullOrEmpty(caseID))
            return false;

        if (cases.ContainsKey(caseID))
            return false;

        cases.Add(caseID, runtime);

        OnCasesChanged?.Invoke();

        return true;
    }

    public List<CaseRuntime> GetAllCases()
    {
        return new List<CaseRuntime>(cases.Values);
    }

    public CaseRuntime GetCase(string id)
    {
        cases.TryGetValue(id, out var c);
        return c;
    }
}