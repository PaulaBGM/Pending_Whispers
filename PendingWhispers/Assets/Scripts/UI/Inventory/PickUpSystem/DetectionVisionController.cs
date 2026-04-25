using System.Collections.Generic;
using UnityEngine;

public class DetectionVisionController : MonoBehaviour
{
    private List<Item> items = new List<Item>();
    private bool isActive = false;

    private void Start()
    {
        items.AddRange(FindObjectsByType<Item>(FindObjectsSortMode.None));
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ToggleVision();
        }
    }

    public void ToggleVision()
    {
        isActive = !isActive;

        foreach (var item in items)
        {
            if (item != null)
                item.SetHighlight(isActive);
        }
    }

    public void RegisterItem(Item item)
    {
        if (!items.Contains(item))
            items.Add(item);
    }
}