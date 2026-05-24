using UnityEngine;
using System.Collections.Generic;
using Inventory.UI;
using Inventory.Model;

public class JournalController : MonoBehaviour
{
    public static JournalController Instance { get; private set; }

    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Pages")]
    [SerializeField] private GameObject inventoryPage;
    [SerializeField] private GameObject peoplePage;
    [SerializeField] private GameObject casesPage;

    [Header("Inventory Controller")]
    [SerializeField] private Inventory.InventoryController inventoryController;

    [Header("Tabs")]
    [SerializeField] private List<InventoryTab> tabs;

    [Header("Animation")]
    [SerializeField] private Animator pageTurnAnimator;

    [Header("Pages Root")]
    [SerializeField] private GameObject pagesRoot;

    private GameObject currentPage;
    private GameObject pendingPage;

    private bool isOpen;
    private bool isAnimating;
    private bool instantOpen;
    private InventoryAnimationEvents animationEvents;
    private GameObject pendingInitialPage;
    
    public bool IsAnimating => isAnimating;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        root.SetActive(false);
        
        animationEvents = GetComponentInChildren<InventoryAnimationEvents>();
        animationEvents.SetUIVisible(false);
    }
    
    private void OnEnable()
    {
        animationEvents.OnOpenFinished += HandleOpenFinished;
        animationEvents.OnCloseFinished += HandleCloseFinished;
    }

    private void HandleCloseFinished()
    {
        currentPage = null;
        pendingPage = null;
        isOpen = false;
        isAnimating = false;
        
        pagesRoot.SetActive(false);
        
        UIManager.Instance.SetJournalOpen(false);

        gameObject.SetActive(false);
    }

    private void HandleOpenFinished()
    {
        isAnimating = false;
        isOpen = true;

        UIManager.Instance.SetJournalOpen(true);
        animationEvents.SetUIVisible(true);

        currentPage = null;
        pendingPage = null;

        if (pendingInitialPage != null)
        {
            SetPageImmediate(pendingInitialPage);
            pendingInitialPage = null;
        }
        else
        {
            SetPageImmediate(inventoryPage);
        }

        RefreshCurrentPage();
    }

    private void Start()
    {
        //FIX 1: suscripción segura
        if (tabs != null)
        {
            foreach (var tab in tabs)
            {
                tab.OnTabSelected += HandleTabSelected;
            }
        }
    }

    // ---------------- TABS ----------------

    private void HandleTabSelected(ItemType type)
    {
        pagesRoot.SetActive(true);

        switch (type)
        {
            case ItemType.Clue:
                RequestPage(inventoryPage);
                break;

            case ItemType.Testimony:
                RequestPage(peoplePage);
                break;

            case ItemType.Case:
                RequestPage(casesPage);
                break;
        }
    }

    // ---------------- TOGGLE ----------------

    public void ToggleJournal()
    {
        if (isAnimating) return;

        isOpen = !isOpen;

        if (isOpen)
        {
            isAnimating = true;
            root.SetActive(isOpen);
        }
        else
        {
            currentPage = null;
            pendingPage = null;
            GetComponentInChildren<InventoryAnimationEvents>().PlayCloseAnimation(root);
        }
    }

    // ---------------- FIRST PAGE FIX ----------------

    private void OpenFirstPage()
    {
        isAnimating = false;

        SetPageImmediate(inventoryPage);

        //FIX CLAVE: asegurar estado inicial correcto
        currentPage = inventoryPage;
        pagesRoot.SetActive(false);

    }

    // ---------------- PAGE REQUEST ----------------

    private void RequestPage(GameObject page)
    {
        if (!isOpen || isAnimating)
            return;
        
        bool samePage = (page == currentPage);

        pendingPage = page;
        isAnimating = true;

        pendingPage = page;
        isAnimating = true;

        HideAllPages();

        if (pageTurnAnimator != null)
            pageTurnAnimator.Play("PageTurnAnimation", 0, 0f);

        if (samePage) pendingPage = currentPage;
    }

    private void RefreshCurrentPage()
    {
        if (currentPage == inventoryPage)
            inventoryController?.ShowInventoryData();

        if (currentPage == peoplePage)
            peoplePage.GetComponent<PeoplePageController>()?.RefreshUI();

        /*if (currentPage == casesPage)
            casesPage.GetComponent<CasesPageController>()?.RefreshUI();*/
    }
    // ---------------- ANIMATION EVENT ----------------

    public void OnPageTurnFinished()
    {
        pagesRoot.SetActive(false);

        if (currentPage != null)
            currentPage.SetActive(false);

        currentPage = pendingPage;
        pendingPage = null;

        if (currentPage != null)
        {
            currentPage.SetActive(true);

            RefreshCurrentPage();
        }

        isAnimating = false;
    }

    // ---------------- INSTANT SET ----------------

    private void SetPageImmediate(GameObject page)
    {
        inventoryPage.SetActive(false);
        peoplePage.SetActive(false);
        casesPage.SetActive(false);

        currentPage = page;
        currentPage.SetActive(true);

        if (currentPage == inventoryPage && inventoryController != null)
            inventoryController.ShowInventoryData();
    }
    
    private void HideAllPages()
    {
        inventoryPage.SetActive(false);
        peoplePage.SetActive(false);
        casesPage.SetActive(false);
    }
    
    public void OpenToCluesTab()
    {
        pendingInitialPage = inventoryPage;

        if (!isOpen)
            ToggleJournal();
        else
            RequestPage(inventoryPage);
    }
    
    public void OpenToPeopleTab()
    {
        pendingInitialPage = peoplePage;

        if (!isOpen)
        {
            ToggleJournal();
        }
        else
        {
            RequestPage(peoplePage);
        }
    }
    
    private void OnDestroy()
    {
        animationEvents.OnOpenFinished -= HandleOpenFinished;
        animationEvents.OnCloseFinished -= HandleCloseFinished;
    }
}