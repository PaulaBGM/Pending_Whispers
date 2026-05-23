using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Inventory.UI
{
    public class UIInventoryDescription : MonoBehaviour
    {
        [SerializeField] private Image itemImage;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text description;

        private void Awake()
        {
            ResetDescription();
        }

        public void ResetDescription()
        {
            if (itemImage != null)
                itemImage.sprite = null;

            if (title != null)
                title.text = "";

            if (description != null)
                description.text = "";
        }

        public void SetDescription(Sprite sprite, string itemName, string itemDescription)
        {
            if (itemImage != null)
            {
                itemImage.sprite = sprite;
                itemImage.enabled = sprite != null;
            }

            if (title != null)
                title.text = itemName;

            if (description != null)
                description.text = itemDescription;
        }
    }
}