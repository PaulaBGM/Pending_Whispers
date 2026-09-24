using UnityEngine;
using UnityEngine.UIElements;

public class CaseBoardPanelUI : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField] private VisualTreeAsset cardTemplate;
    [SerializeField] private CaseDatabaseSO database;
    [SerializeField] private CaseDetailPanelUI detailPanel;

    private VisualElement root;
    private VisualElement cardContainer;

    private void Awake()
    {
        root = document.rootVisualElement.Q<VisualElement>("case-board-panel");
        cardContainer = root.Q<VisualElement>("card-container");
        root.Q<Button>("close-button").clicked += Close;
        Close();
    }

    public void Open()
    {
        root.style.display = DisplayStyle.Flex;
        Refresh();
    }

    public void Close() => root.style.display = DisplayStyle.None;

    private void Refresh()
    {
        cardContainer.Clear();

        foreach (var data in database.allCases)
        {
            bool unlocked = data.unlockFlag == null || GameProgress.Instance.HasFlag(data.unlockFlag);
            bool started = data.startedFlag != null && GameProgress.Instance.HasFlag(data.startedFlag);
            if (!unlocked || started) continue;

            VisualElement card = cardTemplate.CloneTree();
            new CaseBoardCard(card, data, OnCardSelected);
            cardContainer.Add(card);
        }
    }

    private void OnCardSelected(CaseData data) => detailPanel.Open(data, this);
}