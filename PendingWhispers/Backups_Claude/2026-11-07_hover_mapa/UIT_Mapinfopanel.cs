using System.Reflection;
using UnityEngine.UIElements;

/// <summary>
/// Panel de información del mapa (UI Toolkit).
/// - Con el ratón (o el foco) sobre un nodo: muestra su nombre y descripción.
/// - Sin nada encima: muestra el título por defecto del mapa y el objetivo actual del caso.
///
/// Clase C# plana, la crea y la posee el controlador del mapa (UIT_MapController o similar).
/// </summary>
public class UIT_MapInfoPanel
{
    private readonly Label titleLabel;
    private readonly Label descriptionLabel;

    private readonly string defaultTitle;
    private readonly string noCaseText;

    private MapNode hoveredNode;
    private CaseRuntime subscribedCase;

    public UIT_MapInfoPanel(
        VisualElement root,
        string defaultTitle = "Map of the Suburbs of Paris",
        string noCaseText = "",
        string titleLabelName = "L_ZoneTitle",
        string descriptionLabelName = "L_ZoneDescription")
    {
        titleLabel = root.Q<Label>(titleLabelName);
        descriptionLabel = root.Q<Label>(descriptionLabelName);

        if (titleLabel == null)
            UnityEngine.Debug.LogWarning($"[UIT_MapInfoPanel] No se encontró la Label '{titleLabelName}' en el UXML.");
        if (descriptionLabel == null)
            UnityEngine.Debug.LogWarning($"[UIT_MapInfoPanel] No se encontró la Label '{descriptionLabelName}' en el UXML.");

        this.defaultTitle = defaultTitle;
        this.noCaseText = noCaseText;
    }

    // =========================================================
    // REGISTRO DE NODOS
    // =========================================================

    /// <summary>
    /// Engancha hover y foco al botón de un nodo. Llamar una vez por botón al construir el mapa.
    /// PointerLeaveEvent (no PointerOutEvent) para que pasar por los hijos del botón
    /// (icono, label...) no cuente como "salir".
    /// </summary>
    public void RegisterNode(VisualElement button, MapNode node)
    {
        if (button == null || node == null)
            return;

        button.RegisterCallback<PointerEnterEvent>(_ => ShowNode(node));
        button.RegisterCallback<PointerLeaveEvent>(_ => ClearNode(node));

        // Teclado / mando
        button.RegisterCallback<FocusInEvent>(_ => ShowNode(node));
        button.RegisterCallback<FocusOutEvent>(_ => ClearNode(node));
    }

    // =========================================================
    // ESTADOS
    // =========================================================

    public void ShowNode(MapNode node)
    {
        hoveredNode = node;
        SetTexts(node.GetName(), node.GetDescription());
    }

    /// <summary>
    /// Solo vuelve al estado por defecto si el nodo que sale es el que se está mostrando.
    /// Evita parpadeos si el Enter del siguiente botón llega antes que el Leave del anterior.
    /// </summary>
    private void ClearNode(MapNode node)
    {
        if (hoveredNode != node)
            return;

        hoveredNode = null;
        ShowDefault();
    }

    public void ShowDefault()
    {
        SetTexts(defaultTitle, GetCurrentObjectiveText());
    }

    // =========================================================
    // OBJETIVO ACTUAL
    // =========================================================

    /// <summary>Llamar al abrir el mapa.</summary>
    public void Open()
    {
        SubscribeToCurrentCase();
        hoveredNode = null;
        ShowDefault();
    }

    /// <summary>Llamar al cerrar el mapa / OnDisable del controlador.</summary>
    public void Close()
    {
        UnsubscribeFromCase();
        hoveredNode = null;
    }

    // CaseManager guarda el caso activo en un campo privado ("currentCase") sin getter público.
    // Lo leemos por reflexión para no tener que tocar CaseManager. Se cachea una sola vez.
    private static readonly FieldInfo currentCaseField =
        typeof(CaseManager).GetField("currentCase", BindingFlags.Instance | BindingFlags.NonPublic);

    private static CaseRuntime GetCurrentCase()
    {
        if (CaseManager.Instance == null)
            return null;

        if (currentCaseField == null)
        {
            UnityEngine.Debug.LogWarning("[UIT_MapInfoPanel] No se encontró el campo 'currentCase' en CaseManager.");
            return null;
        }

        return currentCaseField.GetValue(CaseManager.Instance) as CaseRuntime;
    }

    private string GetCurrentObjectiveText()
    {
        CaseRuntime currentCase = GetCurrentCase();
        return currentCase != null ? currentCase.GetCurrentObjective() : noCaseText;
    }

    private void SubscribeToCurrentCase()
    {
        CaseRuntime currentCase = GetCurrentCase();
        if (currentCase == subscribedCase)
            return;

        UnsubscribeFromCase();
        subscribedCase = currentCase;

        if (subscribedCase != null)
            subscribedCase.OnCaseUpdated += HandleCaseUpdated;
    }

    private void UnsubscribeFromCase()
    {
        if (subscribedCase != null)
            subscribedCase.OnCaseUpdated -= HandleCaseUpdated;

        subscribedCase = null;
    }

    private void HandleCaseUpdated()
    {
        // Si hay un nodo bajo el ratón no lo pisamos; se verá al salir.
        if (hoveredNode == null)
            ShowDefault();
    }

    private void SetTexts(string title, string description)
    {
        if (titleLabel != null)
            titleLabel.text = title;
        if (descriptionLabel != null)
            descriptionLabel.text = description;
    }
}