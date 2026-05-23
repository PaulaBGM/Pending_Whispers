using System;
using UnityEngine;

public class InventoryAnimationEvents : MonoBehaviour
{
    [Header("UI Root")]
    [SerializeField] private GameObject tabs;
    [SerializeField] private GameObject[] inventoryPages;

    [Header("Start Page")]
    [SerializeField] private GameObject firstPage;
    
    private Animator animator;

    public event Action OnOpenFinished;
    public event Action OnOpenStarted;
    public event Action OnCloseStarted;
    public event Action OnCloseFinished;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        SetUIVisible(false);
    }

    public void OnOpenAnimationStarted()
    {
        OnOpenStarted?.Invoke();
    }
    
    //FIN OPEN BOOK
    public void OnOpenAnimationFinished()
    {
        OnOpenFinished?.Invoke();
    }

    //INICIO CLOSE BOOK
    public void OnCloseAnimationStarted()
    {
        SetUIVisible(false);
        OnCloseStarted?.Invoke();
    }

    public void OnCloseAnimationFinished()
    {
        OnCloseFinished?.Invoke();
    }
    
    public void PlayCloseAnimation(GameObject rootObject)
    {
        animator.SetTrigger("Close");
    }
    
    public void SetUIVisible(bool value)
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