using System.Collections.Generic;
using UnityEngine;

public class CaseJournalSystem : MonoBehaviour
{
    public static CaseJournalSystem Instance;

    private Dictionary<string, CaseData> cases = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        InitializePrototypeCase();
    }

    // ---------------- PROTOTYPE ----------------
    private void InitializePrototypeCase()
    {
        // IMPORTANTE:
        // No crees ScriptableObjects con new
        // Usa uno ya creado en assets o asignado

        CaseData starterCase = ScriptableObject.CreateInstance<CaseData>();

        starterCase.caseID = "case_001";

        cases.Add(starterCase.caseID, starterCase);
    }

    // ---------------- ADD ----------------
    public bool TryAddCase(CaseData newCase)
    {
        if (newCase == null || string.IsNullOrEmpty(newCase.caseID))
            return false;

        if (cases.ContainsKey(newCase.caseID))
            return false;

        cases.Add(newCase.caseID, newCase);
        return true;
    }

    // ---------------- GET BY ID ----------------
    public CaseData GetCase(string id)
    {
        cases.TryGetValue(id, out var caseData);
        return caseData;
    }

    // ---------------- GET ALL ----------------
    public List<CaseData> GetAllCases()
    {
        return new List<CaseData>(cases.Values);
    }
}