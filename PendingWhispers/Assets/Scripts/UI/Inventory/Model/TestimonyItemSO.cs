using UnityEngine;

namespace Inventory.Model
{
    [CreateAssetMenu(menuName = "Inventory/Testimony")]
    public class TestimonyItemSO : ItemSO
    {
        [field: SerializeField] public string Speaker { get; private set; }
        //[field: SerializeField] public EmotionType Emotion { get; private set; }
        [field: SerializeField] public bool IsContradictory { get; private set; }

        [field: SerializeField]
        [TextArea]
        public string Statement { get; private set; }
    }
}