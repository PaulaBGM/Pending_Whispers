using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HubBoardInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private CaseBoardPanelUI boardPanel;

    public void Interact(PlayerController_Actions player)
    {
        boardPanel.Open();
    }

    public Transform GetTransform() => transform;
}