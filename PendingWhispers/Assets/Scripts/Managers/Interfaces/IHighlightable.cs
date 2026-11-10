/// <summary>
/// Objeto del mundo que se resalta cuando el ratón pasa por encima.
/// PlayerController_Actions.HandleHover lo usa para cualquier interactuable que lo implemente.
/// </summary>
public interface IHighlightable
{
    void SetHighlight(bool value);
}
