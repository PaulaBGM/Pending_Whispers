using System.Collections.Generic;
using UnityEngine;

namespace Inventory.Model
{
    [CreateAssetMenu(menuName = "Inventory/Case")]
    public class CaseItemSO : ItemSO
    {
        [field: SerializeField] public string CaseName { get; private set; }

        [field: SerializeField]
        [TextArea]
        public string Summary { get; private set; }

        [field: SerializeField] public int TotalClues { get; private set; } = 5;

        [field: SerializeField] public List<string> DiscoveredClues { get; private set; } = new();

        public int GetProgress()
        {
            return DiscoveredClues.Count;
        }

        public string GetProgressText()
        {
            return $"{DiscoveredClues.Count}/{TotalClues}";
        }
    }
}