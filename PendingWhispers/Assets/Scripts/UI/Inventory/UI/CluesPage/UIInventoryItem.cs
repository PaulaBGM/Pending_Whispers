using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Inventory.UI
{
    public class UIInventoryItem : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IEndDragHandler, IDropHandler, IDragHandler
    {
        [SerializeField] Image itemImage;
        [SerializeField] TMP_Text quantityTxt;
        [SerializeField] private Image borderImage;

        public event Action<UIInventoryItem> OnItemClicked, OnItemDroppedOn,
            OnRightMouseBtnClick, OnItemBeginDrag, OnItemEndDrag;

        public bool empty = true;

        public void Awake()
        {
            ResetData();
            Deselect();
        }

        public void ResetData()
        {
            if (itemImage == null) return;

            itemImage.gameObject.SetActive(false);
            empty = true;
        }

        public void Deselect()
        {
            if (borderImage != null)
                borderImage.enabled = false;
        }

        public void SetData(Sprite sprite, int quantity)
        {
            if (itemImage == null) return;

            itemImage.gameObject.SetActive(true);
            itemImage.sprite = sprite;

            if (quantityTxt != null)
                quantityTxt.text = quantity.ToString();

            empty = false;
        }

        public void Select()
        {
            if (borderImage != null)
                borderImage.enabled = true;
        }

        public void OnPointerClick(PointerEventData pointerData)
        {
            if (pointerData.button == PointerEventData.InputButton.Right)
                OnRightMouseBtnClick?.Invoke(this);
            else
                OnItemClicked?.Invoke(this);
        }

        public void OnDrop(PointerEventData eventData)
        {
            OnItemDroppedOn?.Invoke(this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            OnItemBeginDrag?.Invoke(this);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            OnItemEndDrag?.Invoke(this);
        }

        public void OnDrag(PointerEventData eventData) { }
    }
}