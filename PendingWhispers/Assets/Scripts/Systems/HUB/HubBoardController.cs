using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class HubBoardController : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField] private CaseDatabaseSO database;
    [SerializeField] private CaseDetailPanelController detailPanel; // campo nuevo

    private VisualElement boardRoot;
    private readonly List<PostitSlot> slots = new();

    private void Awake()
    {
        boardRoot = document.rootVisualElement.Q<VisualElement>("HUB_Board_Root");
        var boardBG = boardRoot.Q<VisualElement>("HUB_Board_BG");

        boardBG.Q<Button>("BT_Cancel_Button").clicked += Close;

        for (int i = 1; i <= 8; i++)
        {
            var postit = boardBG.Q<VisualElement>($"HUB_Board_Postit_BG_{i}");
            if (postit != null)
                slots.Add(new PostitSlot(postit, OnPostitClicked));
        }

        Close();
    }

    public void Open()
    {
        boardRoot.style.display = DisplayStyle.Flex;
        Refresh();
    }

    public void Close()
    {
        boardRoot.style.display = DisplayStyle.None;
        detailPanel?.Close();
    }

    private void Refresh()
    {
        var available = database.allCases.Where(c =>
            (c.unlockFlag == null || GameProgress.Instance.HasFlag(c.unlockFlag)) &&
            (c.startedFlag == null || !GameProgress.Instance.HasFlag(c.startedFlag))
        ).ToList();

        for (int i = 0; i < slots.Count; i++)
        {
            if (i < available.Count) slots[i].Bind(available[i]);
            else slots[i].Hide();
        }
    }

    private void OnPostitClicked(CaseData data) => detailPanel.Open(data, OnCaseAccepted);

    private void OnCaseAccepted(CaseData data)
    {
        if (data.startedFlag != null)
            GameProgress.Instance.AddFlag(data.startedFlag);

        CaseManager.Instance.LoadCase(data);
        UIGameEvents.RaiseFeedback($"Caso aceptado: {data.caseTitle}");

        Close();
    }
}