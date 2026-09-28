using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class HubBoardController : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField] private CaseDatabaseSO database;
    [SerializeField] private CaseDetailPanelController detailPanel;

    private VisualElement boardRoot;
    private readonly List<PostitSlot> slots = new();

    private void Awake()
    {
        if (document == null)
        {
            Debug.LogError("[HubBoardController] Document no asignado en el Inspector.");
            return;
        }

        boardRoot = document.rootVisualElement.Q<VisualElement>("HUB_Board_Root");
        if (boardRoot == null)
        {
            Debug.LogError("[HubBoardController] No se encontró 'HUB_Board_Root' en el UXML asignado.");
            return;
        }

        var boardBG = boardRoot.Q<VisualElement>("HUB_Board_BG");

        var cancelButton = boardBG?.Q<Button>("BT_Cancel_Button");
        if (cancelButton != null)
            cancelButton.clicked += Close;
        else
            Debug.LogWarning("[HubBoardController] No se encontró BT_Cancel_Button en HUB_Board_BG.");

        for (int i = 1; i <= 8; i++)
        {
            var postit = boardBG?.Q<VisualElement>($"HUB_Board_Postit_BG_{i}");
            if (postit != null)
                slots.Add(new PostitSlot(postit, OnPostitClicked));
        }

        // Importante: en Awake() solo se oculta la propia pizarra.
        // El orden de Awake() entre distintos GameObjects no está garantizado,
        // así que NO se toca detailPanel aquí (se oculta solo, en su propio Awake).
        boardRoot.style.display = DisplayStyle.None;
    }

    public void Open()
    {
        if (boardRoot == null) return;

        boardRoot.style.display = DisplayStyle.Flex;
        Refresh();
    }

    public void Close()
    {
        if (boardRoot != null)
            boardRoot.style.display = DisplayStyle.None;

        detailPanel?.Close();
    }

    private void Refresh()
    {
        if (database == null)
        {
            Debug.LogError("[HubBoardController] CaseDatabase no asignada en el Inspector.");
            return;
        }

        Debug.Log($"[HubBoardController] Refresh: {slots.Count} slots encontrados en el UXML, " +
            $"{database.allCases?.Count ?? 0} casos en la CaseDatabase.");

        bool hasProgress = GameProgress.Instance != null;
        Debug.Log($"[HubBoardController] GameProgress.Instance {(hasProgress ? "SÍ existe" : "es NULL")}.");

        var available = new List<CaseData>();

        foreach (var c in database.allCases)
        {
            if (c == null)
            {
                Debug.LogWarning("[HubBoardController] Hay un hueco (null) en la lista allCases de la CaseDatabase.");
                continue;
            }

            bool unlockedByFlag = c.unlockFlag == null || (hasProgress && GameProgress.Instance.HasFlag(c.unlockFlag));
            bool notStartedYet = c.startedFlag == null || !(hasProgress && GameProgress.Instance.HasFlag(c.startedFlag));
            bool show = unlockedByFlag || notStartedYet;

            Debug.Log($"[HubBoardController] Caso '{c.caseTitle}' (id={c.caseID}): " +
                $"unlockFlag={(c.unlockFlag == null ? "null" : c.unlockFlag.id)}, " +
                $"startedFlag={(c.startedFlag == null ? "null" : c.startedFlag.id)}, " +
                $"unlockedByFlag={unlockedByFlag}, notStartedYet={notStartedYet} → {(show ? "SE MUESTRA" : "oculto")}");

            if (show) available.Add(c);
        }

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