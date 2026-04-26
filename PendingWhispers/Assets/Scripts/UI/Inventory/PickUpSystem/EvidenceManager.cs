using System.Collections.Generic;
using UnityEngine;

public class EvidenceManager : MonoBehaviour
{
    public static EvidenceManager Instance;

    private HashSet<string> collectedEvidence = new HashSet<string>();

    [Header("Config")]
    public List<string> requiredEvidence;
    public string completedFlag = "all_evidence_collected";

    private bool completed = false; // NEW (evita repetir flag)

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddEvidence(string id)
    {
        if (collectedEvidence.Contains(id)) return;

        collectedEvidence.Add(id);
        Debug.Log("[Evidence] Recogido: " + id);

        CheckCompletion();
    }

    void CheckCompletion()
    {
        if (completed) return; // NEW

        foreach (var req in requiredEvidence)
        {
            if (!collectedEvidence.Contains(req))
                return;
        }

        completed = true; // NEW

        Debug.Log("[Evidence] Todas las pruebas recogidas");

        GameState.Instance.AddFlag(completedFlag); 
    }

    // NEW: útil para debug o UI
    public bool HasEvidence(string id)
    {
        return collectedEvidence.Contains(id);
    }
}