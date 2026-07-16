using UnityEngine;

namespace Inventory.UI
{
    public class MouseFollower : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private ClueEntryUI item;

        private void Awake()
        {
            canvas = transform.root.GetComponent<Canvas>();
            item = GetComponentInChildren<ClueEntryUI>();
        }

        public void SetData(Sprite sprite, int quantity)
        {
            item.SetData(sprite, quantity);
        }

        private void Update()
        {
            Vector2 position;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)canvas.transform,
                Input.mousePosition,
                canvas.worldCamera,
                out position);

            transform.position = canvas.transform.TransformPoint(position);
        }

        public void Toggle(bool val)
        {
            gameObject.SetActive(val);
        }
    }
}