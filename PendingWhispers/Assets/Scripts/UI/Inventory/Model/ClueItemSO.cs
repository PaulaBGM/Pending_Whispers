using UnityEngine;

namespace Inventory.Model
{
    [CreateAssetMenu(menuName = "Inventory/Clue")]
    public class ClueItemSO : ItemSO
    {
        [field: SerializeField] public string ClueID { get; private set; }
        [field: SerializeField] public string LocationFound { get; private set; }
        [field: SerializeField] public bool HasSpectralTrace { get; private set; }
    }
}