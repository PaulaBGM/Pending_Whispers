using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class HubBoardController : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField] private CaseDatabaseSO database;

    private VisualElement boardRoot;
    private readonly List<PostitSlot> slots = new();

    // --- ScaledPostit (panel de detalle grande) ---
    // Vive DENTRO del mismo HUB_Board_Root, como hijo hermano de HUB_Board_BG.
    // Se muestra/oculta con SetEnabled() para disparar la transición CSS
    // ":disabled { scale: 0 0; }" definida en HUB_Board_USS.uss — por eso
    // nunca se debe tocar su style.display a mano.
    private VisualElement scaledPostit;
    private Label scaledCaseName;
    private Label scaledApplicantName;
    private Label scaledApplicantLocation;
    private Label scaledRequestDescription;
    private Label scaledRewardDescription;
    private Button acceptButton;
    private Button scaledCancelButton;

    private CaseData currentDetailData;

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

        // UXML tiene dos botones con el mismo nombre "BT_Cancel_Button":
        // uno dentro de HUB_Board_BG (cierra la pizarra) y otro dentro de
        // HUB_ScaledPostit_BG (cierra solo el detalle). Hay que acotar por
        // contenedor para no coger el que no toca.
        var cancelButton = boardBG?.Q<Button>("BT_Cancel_Button");
        if (cancelButton != null)
        {
            cancelButton.clicked += () =>
            {
                Debug.Log("[HubBoardController] CLICK recibido en BT_Cancel_Button (pizarra).");
                Close();
            };
        }
        else
        {
            Debug.LogWarning("[HubBoardController] No se encontró BT_Cancel_Button en HUB_Board_BG.");
        }

        // Diagnóstico global: registra CUALQUIER puntero que baje sobre el
        // documento entero, en fase de captura (TrickleDown), antes de que
        // nada pueda interceptarlo o detener la propagación. Si esto NO
        // aparece nunca en consola al hacer click sobre la pizarra, el
        // problema es que el input no está llegando al UIDocument en
        // absoluto (EventSystem / Panel Settings / otro panel encima),
        // no un fallo de lógica en los botones.
        document.rootVisualElement.RegisterCallback<PointerDownEvent>(evt =>
        {
            Debug.Log($"[HubBoardController] (GLOBAL) PointerDownEvent — target: {evt.target}, " +
                $"posición: {evt.position}.");
        }, TrickleDown.TrickleDown);

        for (int i = 1; i <= 8; i++)
        {
            var postit = boardBG?.Q<VisualElement>($"HUB_Board_Postit_BG_{i}");
            if (postit != null)
                slots.Add(new PostitSlot(postit, OnPostitClicked));
        }

        SetupScaledPostit();

        // En Awake() solo se oculta la propia pizarra (el ScaledPostit se
        // deja ya cerrado dentro de SetupScaledPostit).
        boardRoot.style.display = DisplayStyle.None;
    }

    private void SetupScaledPostit()
    {
        scaledPostit = boardRoot.Q<VisualElement>("HUB_ScaledPostit_BG");
        if (scaledPostit == null)
        {
            Debug.LogError("[HubBoardController] No se encontró 'HUB_ScaledPostit_BG' dentro de HUB_Board_Root.");
            return;
        }

        scaledCaseName = scaledPostit.Q<Label>("T_ScaledPostit_CaseName");
        scaledApplicantName = scaledPostit.Q<Label>("T_Applicant_Name");
        scaledApplicantLocation = scaledPostit.Q<Label>("T_Applicant_Location");
        scaledRequestDescription = scaledPostit.Q<Label>("T_RequestDescription");
        scaledRewardDescription = scaledPostit.Q<Label>("T_Reward_Description");
        acceptButton = scaledPostit.Q<Button>("BT_AcceptCase");
        scaledCancelButton = scaledPostit.Q<Button>("BT_Cancel_Button");

        if (acceptButton != null)
            acceptButton.clicked += () => OnCaseAccepted(currentDetailData);
        else
            Debug.LogWarning("[HubBoardController] No se encontró BT_AcceptCase en HUB_ScaledPostit_BG.");

        if (scaledCancelButton != null)
            scaledCancelButton.clicked += CloseDetail;
        else
            Debug.LogWarning("[HubBoardController] No se encontró BT_Cancel_Button en HUB_ScaledPostit_BG.");

        // Estado inicial cerrado: enabled = false aplica la pseudo-clase
        // :disabled (scale 0 0) definida en el USS.
        //
        // IMPORTANTE: "scale" es una transformación VISUAL — no cambia el
        // layout. Aunque el panel esté reducido a 0 0 (invisible), su
        // hitbox de picking sigue siendo el 50% x 70% centrado original,
        // y al estar declarado DESPUÉS de HUB_Board_BG en el UXML queda
        // por encima en el árbol de picking. Resultado: aunque no se vea,
        // se come los clics de todo lo que haya debajo (post-its, la X...).
        // Por eso hay que desactivar también su picking mode mientras
        // está cerrado, y reactivarlo solo cuando se abre.
        scaledPostit.SetEnabled(false);
        scaledPostit.pickingMode = PickingMode.Ignore;
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

        CloseDetail();
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
            bool show = unlockedByFlag && notStartedYet;

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

    private void OnPostitClicked(CaseData data) => OpenDetail(data);

    private void OpenDetail(CaseData data)
    {
        if (scaledPostit == null) return;

        currentDetailData = data;

        if (scaledCaseName != null) scaledCaseName.text = data.caseTitle;
        if (scaledApplicantName != null) scaledApplicantName.text = data.requesterName;
        if (scaledApplicantLocation != null) scaledApplicantLocation.text = data.requesterLocation;
        if (scaledRequestDescription != null) scaledRequestDescription.text = data.requestSummary;
        if (scaledRewardDescription != null) scaledRewardDescription.text = $"+{data.baseReputationReward}% de reputación";

        // SetEnabled(true) quita la pseudo-clase :disabled → el USS anima
        // el crecimiento desde el centro (scale 0 0 -> 1 1). Reactivamos
        // también el picking, que estaba desactivado mientras el panel
        // estaba cerrado (ver comentario en SetupScaledPostit).
        scaledPostit.pickingMode = PickingMode.Position;
        scaledPostit.SetEnabled(true);
        scaledPostit.BringToFront();
    }

    private void CloseDetail()
    {
        currentDetailData = null;

        if (scaledPostit == null) return;

        scaledPostit.SetEnabled(false);
        scaledPostit.pickingMode = PickingMode.Ignore;
    }

    private void OnCaseAccepted(CaseData data)
    {
        if (data == null) return;

        if (data.startedFlag != null)
            GameProgress.Instance.AddFlag(data.startedFlag);

        CaseManager.Instance.LoadCase(data);
        UIGameEvents.RaiseFeedback($"Caso aceptado: {data.caseTitle}");

        Close();
    }
}