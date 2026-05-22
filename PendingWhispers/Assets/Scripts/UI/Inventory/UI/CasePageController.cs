using UnityEngine;

public class CasesPageController : MonoBehaviour
{
    [SerializeField] private GameObject listPanel;
    [SerializeField] private GameObject hypothesisPanel;
    [SerializeField] private GameObject summaryPanel;

    public void SelectCase()
    {
        listPanel.SetActive(false);
        hypothesisPanel.SetActive(true);
        summaryPanel.SetActive(true);
    }

    public void BackToList()
    {
        listPanel.SetActive(true);
        hypothesisPanel.SetActive(false);
        summaryPanel.SetActive(false);
    }
}