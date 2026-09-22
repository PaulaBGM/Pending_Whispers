using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class HubBoardUIToolkitController : MonoBehaviour
{
    private const string BoardRootName = "HUB_Board_Root";
    private const string DetailRootName = "HUB_ScaledPostit_BG";
    private const string CaseTitleName = "T_Postit_CaseName";
    private const string CaseInformationName = "T_Postit_Information";
    private const string ReputationName = "T_ReputationLabel";

    [SerializeField] private CaseDatabaseSO database;

    private readonly List<Button> caseButtons = new();
    private UIDocument document;
    private VisualElement boardRoot;
    private VisualElement detailRoot;
    private Label detailTitle;
    private Label applicantName;
    private Label applicantLocation;
    private Label requestDescription;
    private Label rewardDescription;
    private CaseData selectedCase;

    private void Awake()
    {
        document = GetComponent<UIDocument>();
        CacheVisualElements();
    }

    private void Start()
    {
        Open();
    }

    private void OnEnable()
    {
        if (boardRoot != null)
            boardRoot.RegisterCallback<KeyDownEvent>(OnKeyDown);
    }

    private void OnDisable()
    {
        if (boardRoot != null)
            boardRoot.UnregisterCallback<KeyDownEvent>(OnKeyDown);
    }

    public void Open()
    {
        if (boardRoot == null)
            CacheVisualElements();

        if (boardRoot == null)
            return;

        RefreshCases();
        detailRoot.style.display = DisplayStyle.None;
        boardRoot.style.display = DisplayStyle.Flex;
        boardRoot.Focus();
    }

    public void Hide()
    {
        selectedCase = null;

        if (detailRoot != null)
            detailRoot.style.display = DisplayStyle.None;

        if (boardRoot != null)
            boardRoot.style.display = DisplayStyle.None;
    }

    private void CacheVisualElements()
    {
        boardRoot = document.rootVisualElement.Q<VisualElement>(BoardRootName);
        if (boardRoot == null)
        {
            Debug.LogError("[HubBoard] The board UXML root could not be found.", this);
            return;
        }

        detailRoot = boardRoot.Q<VisualElement>(DetailRootName);
        detailTitle = boardRoot.Q<Label>("T_ScaledPostit_CaseName");
        applicantName = boardRoot.Q<Label>("T_Applicant_Name");
        applicantLocation = boardRoot.Q<Label>("T_Applicant_Location");
        requestDescription = boardRoot.Q<Label>("T_RequestDescription");
        rewardDescription = boardRoot.Q<Label>("T_Reward_Description");

        caseButtons.Clear();
        foreach (var button in boardRoot.Query<Button>(className: "hubPostItBG").ToList())
        {
            caseButtons.Add(button);
            button.clicked += () => OpenCase(button.userData as CaseData);
        }

        boardRoot.Q<Button>("BT_Cancel_Button")?.RegisterCallback<ClickEvent>(_ => CloseDetail());
        boardRoot.Q<Button>("BT_AcceptCase")?.RegisterCallback<ClickEvent>(_ => AcceptSelectedCase());
    }

    private void RefreshCases()
    {
        var availableCases = GetAvailableCases();

        for (var i = 0; i < caseButtons.Count; i++)
        {
            var button = caseButtons[i];
            var hasCase = i < availableCases.Count;
            button.style.display = hasCase ? DisplayStyle.Flex : DisplayStyle.None;
            button.userData = hasCase ? availableCases[i] : null;

            if (!hasCase)
                continue;

            var caseData = availableCases[i];
            button.Q<Label>(CaseTitleName).text = caseData.caseTitle;
            button.Q<Label>(CaseInformationName).text = $"- {caseData.requesterName}\n\n- {caseData.requesterLocation}";
            button.Q<Label>(ReputationName).text = $"Reputation: +{caseData.baseReputationReward}%";
            button.Q<VisualElement>("HUB_Board_Postit_CaseState").style.display = DisplayStyle.None;
        }
    }

    private List<CaseData> GetAvailableCases()
    {
        var availableCases = new List<CaseData>();
        if (database == null || database.allCases == null)
        {
            Debug.LogWarning("[HubBoard] No case database has been assigned.", this);
            return availableCases;
        }

        foreach (var caseData in database.allCases)
        {
            if (caseData == null)
                continue;

            var isUnlocked = caseData.unlockFlag == null || GameProgress.Instance.HasFlag(caseData.unlockFlag);
            var hasStarted = caseData.startedFlag != null && GameProgress.Instance.HasFlag(caseData.startedFlag);
            if (isUnlocked && !hasStarted)
                availableCases.Add(caseData);
        }

        return availableCases;
    }

    private void OpenCase(CaseData caseData)
    {
        if (caseData == null)
            return;

        selectedCase = caseData;
        detailTitle.text = caseData.caseTitle;
        applicantName.text = caseData.requesterName;
        applicantLocation.text = caseData.requesterLocation;
        requestDescription.text = caseData.requestSummary;
        rewardDescription.text = $"+{caseData.baseReputationReward}% reputation";
        detailRoot.style.display = DisplayStyle.Flex;
    }

    private void CloseDetail()
    {
        selectedCase = null;
        detailRoot.style.display = DisplayStyle.None;
    }

    private void AcceptSelectedCase()
    {
        if (selectedCase == null)
            return;

        if (selectedCase.startedFlag != null)
            GameProgress.Instance.AddFlag(selectedCase.startedFlag);

        CaseManager.Instance.LoadCase(selectedCase);
        UIGameEvents.RaiseFeedback($"Caso aceptado: {selectedCase.caseTitle}");
        Hide();
    }

    private void OnKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode != KeyCode.Escape)
            return;

        if (selectedCase != null)
            CloseDetail();
        else
            Hide();

        evt.StopPropagation();
    }
}
