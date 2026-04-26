using System;
using UnityEngine;

namespace Inventory.Model
{
    [Serializable]
    public class EvidenceLink: MonoBehaviour
    {
        public ItemSO ItemA;
        public ItemSO ItemB;

        [TextArea]
        public string Conclusion;
    }
}