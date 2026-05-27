using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

    private InventoryAnimationEvents animationEvents;
    private GameObject pendingInitialPage;

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

    private void OnDestroy()
    {
        if (animationEvents != null)
        {
            animationEvents.OnOpenFinished -= HandleOpenFinished;
            animationEvents.OnCloseFinished -= HandleCloseFinished;
        }
    }

    // =========================
    // OPEN / CLOSE
    // =========================

    public void ToggleJournal()
    {
        if (isAnimating) return;

        isOpen = !isOpen;

        if (isOpen)
        {
            isAnimating = true;
            root.SetActive(true);
        }
        else
        {
            currentPage = null;
            pendingPage = null;

            animationEvents.PlayCloseAnimation(root);
        }
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

        StartCoroutine(RefreshAfterOpen());
    }

    private IEnumerator RefreshAfterOpen()
    {
        yield return null;
        RefreshCurrentPage();
    }

    // =========================
    // TABS
    // =========================

    private void Start()
    {
        if (tabs == null) return;

        foreach (var tab in tabs)
            tab.OnTabSelected += HandleTabSelected;
    }

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

    // =========================
    // PAGE REQUEST
    // =========================

    private void RequestPage(GameObject page)
    {
        if (!isOpen || isAnimating) return;

        pendingPage = page;
        isAnimating = true;

        HideAllPages();

        if (pageTurnAnimator != null)
            pageTurnAnimator.Play("PageTurnAnimation", 0, 0f);
    }

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
            StartCoroutine(RefreshPageNextFrame());
        }

        isAnimating = false;
    }

    private IEnumerator RefreshPageNextFrame()
    {
        yield return null;
        RefreshCurrentPage();
    }

    // =========================
    // PAGE REFRESH LOGIC
    // =========================

    private void RefreshCurrentPage()
    {
        if (currentPage == inventoryPage)
            inventoryController?.RefreshUI();

        if (currentPage == peoplePage)
            peoplePage.GetComponent<PeoplePageController>()?.RefreshUI();

        if (currentPage == casesPage)
            casesPage.GetComponent<CasePageController>()?.RefreshUI();
    }

    // =========================
    // SET PAGE IMMEDIATE
    // =========================

    private void SetPageImmediate(GameObject page)
    {
        inventoryPage.SetActive(false);
        peoplePage.SetActive(false);
        casesPage.SetActive(false);

        currentPage = page;
        currentPage.SetActive(true);
    }

    private void HideAllPages()
    {
        inventoryPage.SetActive(false);
        peoplePage.SetActive(false);
        casesPage.SetActive(false);
    }

    // =========================
    // EXTERNAL OPENERS
    // =========================

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
            ToggleJournal();
        else
            RequestPage(peoplePage);
    }
}