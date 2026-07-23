using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;

public class HypothesisSlotUI : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;
    public event Action<int, string> OnValueChanged;
    private int slotIndex;

    private void Start()
    {
        dropdown = GetComponent<TMP_Dropdown>();
    }

    public void Init(int index, List<string> options)
    {
        slotIndex = index;
        dropdown.ClearOptions();
        dropdown.AddOptions(options);
        dropdown.onValueChanged.AddListener(OnChanged);
    }

    private void OnChanged(int value)
    {
        OnValueChanged?.Invoke(slotIndex, dropdown.options[value].text);
    }
}