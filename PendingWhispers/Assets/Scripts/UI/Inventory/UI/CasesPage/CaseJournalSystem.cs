using System;
using System.Collections.Generic;
using UnityEngine;

public class CaseJournalSystem : MonoBehaviour
{
    public static CaseJournalSystem Instance;

    private Dictionary<string, CaseRuntime> cases = new();

    public event Action OnCasesChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    public bool TryAddCase(CaseData newCase)
    {
        if (newCase == null || string.IsNullOrEmpty(newCase.caseID))
            return false;

        if (cases.ContainsKey(newCase.caseID))
            return false;

        cases.Add(newCase.caseID, new CaseRuntime(newCase));

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