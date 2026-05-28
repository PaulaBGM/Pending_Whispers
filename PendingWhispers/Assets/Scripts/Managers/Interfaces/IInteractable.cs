using UnityEngine;

public interface IInteractable
{
    void Interact(PlayerController_Actions player);
    Transform GetTransform();
    
}