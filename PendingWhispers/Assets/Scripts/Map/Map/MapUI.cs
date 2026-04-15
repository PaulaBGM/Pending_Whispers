using UnityEngine;
using TMPro;

public class MapUI : MonoBehaviour
{
    public static MapUI Instance;

    public TextMeshProUGUI zoneTitle;

    private void Awake()
    {
        Instance = this;
    }

    public void UpdateTitle(string newTitle)
    {
        zoneTitle.text = newTitle;
    }
}