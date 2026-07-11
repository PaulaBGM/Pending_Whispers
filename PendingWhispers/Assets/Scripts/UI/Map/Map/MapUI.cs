using UnityEngine;
using TMPro;

public class MapUI : BaseSingleton<MapUI>
{
    protected override bool PersistAcrossScenes => false;

    public TextMeshProUGUI zoneTitle;
    public TextMeshProUGUI zoneDesc;

    public void UpdateTitle(string newTitle, string newDescription)
    {
        zoneTitle.text = newTitle;
        zoneDesc.text = newDescription;

    }
}