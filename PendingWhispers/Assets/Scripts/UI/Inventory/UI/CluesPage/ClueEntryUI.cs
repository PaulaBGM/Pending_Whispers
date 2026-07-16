using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Inventory.UI
{
    public class ClueEntryUI : JournalEntryUI<Sprite>,
        IBeginDragHandler,
        IEndDragHandler,
        IDropHandler,
        IDragHandler
    {
        [Header("UI")]
        [SerializeField] private Image itemImage;
        [SerializeField] private TMP_Text quantityTxt;

        public event Action<ClueEntryUI> OnItemClicked;
        public event Action<ClueEntryUI> OnItemDroppedOn;
        public event Action<ClueEntryUI> OnRightMouseBtnClick;
        public event Action<ClueEntryUI> OnItemBeginDrag;
        public event Action<ClueEntryUI> OnItemEndDrag;

        public bool Empty { get; private set; } = true;

        public override void ResetData()
        {
            base.ResetData();

            if (itemImage != null)
            {
                itemImage.sprite = null;
                itemImage.gameObject.SetActive(false);
            }

            if (quantityTxt != null)
                quantityTxt.text = "";

            Empty = true;
        }

        public void SetData(Sprite sprite, int quantity)
        {
            if (sprite == null)
            {
                ResetData();
                return;
            }

            SetEntry(sprite);

            itemImage.gameObject.SetActive(true);
            itemImage.sprite = sprite;

            if (quantityTxt != null)
                quantityTxt.text = quantity.ToString();

            Empty = false;
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
                OnRightMouseBtnClick?.Invoke(this);
            else
                OnItemClicked?.Invoke(this);

            base.OnPointerClick(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            OnItemBeginDrag?.Invoke(this);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            OnItemEndDrag?.Invoke(this);
        }

        public void OnDrop(PointerEventData eventData)
        {
            OnItemDroppedOn?.Invoke(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
        }
    }
}