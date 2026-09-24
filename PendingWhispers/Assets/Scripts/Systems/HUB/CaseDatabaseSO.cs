using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Case/Case Database")]
public class CaseDatabaseSO : ScriptableObject
{
    public List<CaseData> allCases;
}