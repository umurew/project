using UnityEngine;

public interface IInteractable
{
    void Interact();
    string Description();
    Transform GetUIAnchorPosition();
}
