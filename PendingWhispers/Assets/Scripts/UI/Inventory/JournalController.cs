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

    public bool IsAnimating => isAnimating;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        root.SetActive(false);
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
        Debug.Log("TAB RECEIVED: " + type);

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
        root.SetActive(isOpen);

        if (isOpen)
        {
            OpenFirstPage();
        }
        else
        {
            currentPage = null;
            pendingPage = null;
        }
    }

    // ---------------- FIRST PAGE FIX ----------------

    private void OpenFirstPage()
    {
        isAnimating = false;

        SetPageImmediate(inventoryPage);

        //FIX CLAVE: asegurar estado inicial correcto
        currentPage = inventoryPage;
    }

    // ---------------- PAGE REQUEST ----------------

    private void RequestPage(GameObject page)
    {
        Debug.Log("REQUEST PAGE: " + page.name);
        if (!isOpen) return;
        if (isAnimating) return;
        if (page == currentPage) return;

        pendingPage = page;
        isAnimating = true;

        HideAllPages();

        if (pageTurnAnimator != null)
            pageTurnAnimator.Play("PageTurnAnimation", 0, 0f);
    }

    // ---------------- ANIMATION EVENT ----------------

    public void OnPageTurnFinished()
    {
        Debug.Log("PAGE TURN FINISHED");

        pagesRoot.SetActive(false);
        if (currentPage != null)
            currentPage.SetActive(false);

        currentPage = pendingPage;
        pendingPage = null;

        if (currentPage != null)
        {
            currentPage.SetActive(true);

            if (currentPage == inventoryPage && inventoryController != null)
                inventoryController.ShowInventoryData();
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
}