using UnityEngine;

public class CaseBoardPanelUI : MonoBehaviour
{
    [SerializeField] private CaseDatabaseSO database;
    [SerializeField] private CaseBoardCardUI cardPrefab;
    [SerializeField] private Transform cardContainer;
    [SerializeField] private CaseDetailPanelUI detailPanel;
    [SerializeField] private GameObject panelRoot;

    public void Open()
    {
        panelRoot.SetActive(true);
        Refresh();
    }

    public void Close() => panelRoot.SetActive(false);

    private void Refresh()
    {
        foreach (Transform child in cardContainer) Destroy(child.gameObject);

        foreach (var data in database.allCases)
        {
            bool unlocked = data.unlockFlag == null || GameProgress.Instance.HasFlag(data.unlockFlag);
            bool alreadyStarted = data.startedFlag != null && GameProgress.Instance.HasFlag(data.startedFlag);

            if (!unlocked || alreadyStarted) continue;

            var card = Instantiate(cardPrefab, cardContainer);
            card.Setup(data, OnCardSelected);
        }
    }

    private void OnCardSelected(CaseData data) => detailPanel.Open(data, this);
}