using UnityEngine;

public interface IInteractable
{
    void Interact(PlayerController_MovementInteraction player);
    Transform GetTransform();
    
}