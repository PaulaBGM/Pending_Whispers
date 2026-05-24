using UnityEngine;

public class InventoryPageAnimEvent : MonoBehaviour
{
    [Header("Page Content (UI real)")]
    [SerializeField] private GameObject pageContent;

    private void Awake()
    {
        //al empezar animación: ocultamos contenido
        if (pageContent != null)
            pageContent.SetActive(false);
    }

    //llamado al inicio de animación TAB / flip
    public void OnAnimationStarted()
    {
        if (pageContent != null)
            pageContent.SetActive(false);
    }

    //llamado al final de animación
    public void OnAnimationFinished()
    {
        JournalController.Instance.OnPageTurnFinished();
    }
}