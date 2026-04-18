using System;
using UnityEngine;
using UnityEngine.UI;

namespace Inventory.UI
{
    public class ItemActionPanel : MonoBehaviour
    {
        [SerializeField] private GameObject buttonPrefab;

        public void AddButon(string name, Action onClickAction)
        {
            GameObject button = Instantiate(buttonPrefab, transform);

            Button btn = button.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => onClickAction());

            button.GetComponentInChildren<TMPro.TMP_Text>().text = name;
        }

        public void Toggle(bool val)
        {
            if (val)
                RemoveOldButtons();

            gameObject.SetActive(val);
        }

        private void RemoveOldButtons()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }
    }
}