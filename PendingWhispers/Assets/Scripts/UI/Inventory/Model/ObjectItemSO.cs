using UnityEngine;

namespace Inventory.Model
{
    /// <summary>
    /// Objeto de inventario usable en el escenario (llave, ron, herramienta...).
    /// Va en la pestaña de objetos del diario: en el inspector, ItemType = Item.
    /// De momento no añade campos: lo que se hace con el objeto lo define el
    /// hotspot que lo acepta, no el propio objeto.
    /// </summary>
    [CreateAssetMenu(menuName = "Inventory/Object")]
    public class ObjectItemSO : ItemSO
    {
    }
}