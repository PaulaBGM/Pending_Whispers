using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HubBoardInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private HubBoardController board;

    public void Interact(PlayerController_Actions player) => board.Open();
    public Transform GetTransform() => transform;
}