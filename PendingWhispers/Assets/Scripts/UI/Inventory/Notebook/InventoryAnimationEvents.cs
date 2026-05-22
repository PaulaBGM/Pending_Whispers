using UnityEngine;

public class InventoryAnimationEvents : MonoBehaviour
{
    [Header("UI Root")]
    [SerializeField] private GameObject tabs;
    [SerializeField] private GameObject[] inventoryPages;

    [Header("Start Page")]
    [SerializeField] private GameObject firstPage;

    private void Awake()
    {
        SetUIVisible(false);
    }

    //FIN OPEN BOOK
    public void OnOpenAnimationFinished()
    {
        SetUIVisible(true);
        OpenFirstPage();
    }

    //INICIO CLOSE BOOK
    public void OnCloseAnimationStarted()
    {
        SetUIVisible(false);
    }

    private void SetUIVisible(bool value)
    {
        if (tabs != null)
            tabs.SetActive(value);

        if (inventoryPages == null) return;

        foreach (var page in inventoryPages)
        {
            if (page != null)
                page.SetActive(value);
        }
    }

    private void OpenFirstPage()
    {
        if (inventoryPages == null) return;

        foreach (var page in inventoryPages)
        {
            if (page != null)
                page.SetActive(false);
        }

        if (firstPage != null)
            firstPage.SetActive(true);
    }
}