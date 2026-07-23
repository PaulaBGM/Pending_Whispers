using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Inventory.UI;
using Inventory.Model;
using FMODUnity;
using FMOD.Studio;

public class JournalController : BaseSingleton<JournalController>
{
    protected override bool PersistAcrossScenes => false;

    public bool IsOpen => isOpen;

    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Pages")]
    [SerializeField] private GameObject instructionsPage;
    [SerializeField] private GameObject inventoryPage;
    [SerializeField] private GameObject peoplePage;
    [SerializeField] private GameObject casesPage;
    private GameObject lastRequestedPage;

    [Header("Inventory Controller")]
    [SerializeField] private Inventory.InventoryController inventoryController;

    [Header("Tabs")]
    [SerializeField] private List<InventoryTab> tabs;

    [Header("Animation")]
    [SerializeField] private Animator pageTurnAnimator;

    [Header("Pages Root")]
    [SerializeField] private GameObject pageAnimation;

    private GameObject currentPage;
    private GameObject pendingPage;
    private bool isOpen;
    private bool isAnimating;

    private InventoryAnimationEvents animationEvents;
    private GameObject pendingInitialPage;

    [Header("Audio")]
    [SerializeField] private EventReference pageTurnSFX;

    [Header("Events")]
    [SerializeField] private BoolEventChannelSO onJournalStateChannel;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
            return;

        root.SetActive(false);
        animationEvents = GetComponentInChildren<InventoryAnimationEvents>();
        if (animationEvents != null)
            animationEvents.SetUIVisible(false);
    }

    private void OnEnable()
    {
        if (animationEvents != null)
        {
            animationEvents.OnOpenFinished += HandleOpenFinished;
            animationEvents.OnCloseFinished += HandleCloseFinished;
        }
    }

    protected override void OnDestroy()
    {
        if (animationEvents != null)
        {
            animationEvents.OnOpenFinished -= HandleOpenFinished;
            animationEvents.OnCloseFinished -= HandleCloseFinished;
        }
        base.OnDestroy();
    }

    private void Start()
    {
        if (tabs == null) return;
        foreach (var tab in tabs)
            tab.OnTabSelected += HandleTabSelected;
    }

    // =========================
    // OPEN / CLOSE
    // =========================
    public void ToggleJournal()
    {
        if (isAnimating)
            return;

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

            if (animationEvents != null)
                animationEvents.PlayCloseAnimation(root);
        }
    }

    private void HandleOpenFinished()
    {
        isAnimating = false;
        isOpen = true;

        onJournalStateChannel?.Raise(true);

        if (animationEvents != null)
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

        if (isActiveAndEnabled)
            StartCoroutine(RefreshAfterOpen());
        else
            RefreshCurrentPage();

        TutorialPopup.Instance.ShowTutorialOnce("journal", "Journal", "Here you'll find clues, testimonies, known people, and hypotheses. Use this information to reconstruct each case.");
    }

    private void HandleCloseFinished()
    {
        currentPage = null;
        pendingPage = null;
        isOpen = false;
        isAnimating = false;

        if (pageAnimation != null)
            pageAnimation.SetActive(false);

        onJournalStateChannel?.Raise(false);

        gameObject.SetActive(false);
    }

    private IEnumerator RefreshAfterOpen()
    {
        RefreshCurrentPage();
        yield return null;
    }

    // =========================
    // TABS
    // =========================
    private void HandleTabSelected(ItemType type)
    {
        GameObject targetPage = GetPageFromType(type);

        if (targetPage == null || !isOpen)
            return;
        if (currentPage == targetPage)
            return;
        if (pendingPage == targetPage)
            return;

        if (pageAnimation != null)
            pageAnimation.SetActive(true);

        RequestPage(targetPage);
    }

    private GameObject GetPageFromType(ItemType type)
    {
        switch (type)
        {
            case ItemType.Instructions:
                return instructionsPage;
            case ItemType.Clue:
                return inventoryPage;
            case ItemType.Testimony:
                return peoplePage;
            case ItemType.Case:
                return casesPage;
            default:
                return null;
        }
    }

    // =========================
    // PAGE REQUEST
    // =========================
    private void RequestPage(GameObject page)
    {
        if (!isOpen || page == null)
            return;
        if (currentPage == page || pendingPage == page)
            return;

        pendingPage = page;
        isAnimating = true;

        HideAllPages();

        if (pageTurnAnimator != null)
            pageTurnAnimator.Play("PageTurnAnimation", 0, 0f);

        if (!pageTurnSFX.IsNull)
        {
            RuntimeManager.PlayOneShot(pageTurnSFX);
        }
    }

    public void OnPageTurnFinished()
    {
        isAnimating = false;

        if (pendingPage == null)
            return;

        if (pageAnimation != null)
            pageAnimation.SetActive(false);

        if (currentPage != null)
            currentPage.SetActive(false);

        currentPage = pendingPage;
        pendingPage = null;
        currentPage.SetActive(true);

        StartCoroutine(RefreshPageNextFrame());
        isAnimating = false;
    }

    private IEnumerator RefreshPageNextFrame()
    {
        yield return null;
        RefreshCurrentPage();
    }

    // =========================
    // REFRESH
    // =========================
    private void RefreshCurrentPage()
    {
        if (currentPage == inventoryPage && inventoryController != null)
            inventoryController.RefreshUI();

        if (currentPage == peoplePage)
            peoplePage.GetComponent<PeoplePageController>()?.Refresh();

        if (currentPage == casesPage)
            casesPage.GetComponent<CasePageController>()?.Refresh();
    }

    // =========================
    // HELPERS
    // =========================
    private void SetPageImmediate(GameObject page)
    {
        HideAllPages();
        currentPage = page;
        if (currentPage != null)
            currentPage.SetActive(true);
    }

    private void HideAllPages()
    {
        if (instructionsPage != null) instructionsPage.SetActive(false);
        if (inventoryPage != null) inventoryPage.SetActive(false);
        if (peoplePage != null) peoplePage.SetActive(false);
        if (casesPage != null) casesPage.SetActive(false);
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