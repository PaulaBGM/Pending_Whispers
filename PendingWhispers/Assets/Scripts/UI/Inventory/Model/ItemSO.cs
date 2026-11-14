using UnityEngine;

namespace Inventory.Model
{
    /// <summary>
    /// Base de todo lo que va al cuaderno: pistas, testimonios, objetos y casos.
    /// Solo guarda lo que comparten todos; lo específico va en cada subclase.
    /// </summary>
    public abstract class ItemSO : ScriptableObject
    {
        [field: SerializeField] public string Name { get; private set; }

        [field: SerializeField, Tooltip("Texto que aparece como opción en el panel de hipótesis.")]
        public string NameHypothesis { get; private set; }

        [field: SerializeField, TextArea]
        public string Description { get; private set; }

        [field: SerializeField] public Sprite ItemImage { get; private set; }

        [field: SerializeField, Tooltip("Pestaña del cuaderno en la que aparece.")]
        public ItemType ItemType { get; private set; }

        [field: SerializeField, Tooltip("Caso al que pertenece. Aún no lo lee ningún script.")]
        public string CaseID { get; private set; }
    }
}
