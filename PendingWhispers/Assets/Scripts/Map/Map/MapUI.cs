using UnityEngine;
using TMPro;

public class MapUI : MonoBehaviour
{
    public static MapUI Instance;

    public TextMeshProUGUI zoneTitle;
    public TextMeshProUGUI zoneDesc;

    private void Awake()
    {
        Instance = this;
    }

    public void UpdateTitle(string newTitle, string newDescription)
    {
        zoneTitle.text = newTitle;
        zoneDesc.text = newDescription;

    }
}