using UnityEngine;

namespace Inventory.Model
{
    [CreateAssetMenu(menuName = "Inventory/Sample")]
    public class SampleItemSO : ItemSO
    {
        [field: SerializeField] public bool IsAnalyzed { get; set; }

        [field: SerializeField]
        [TextArea]
        public string AnalysisResult { get; private set; }
    }
}