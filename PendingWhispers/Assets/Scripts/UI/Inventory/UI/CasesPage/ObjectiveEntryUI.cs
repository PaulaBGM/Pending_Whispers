using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ObjectiveEntryUI : MonoBehaviour
{
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private Image completedIcon;

    public void SetData(string text, bool completed)
    {
        objectiveText.text = text;

        if (completedIcon != null)
            completedIcon.enabled = completed;
    }
}