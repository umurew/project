using UnityEngine;

[RequireComponent(typeof(EntityBlackboard))]
public class PlayerInteraction : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private float _interactionDistance = 2.5f;
    [SerializeField] private LayerMask _interactionMask;

    private IInputProvider _inputProvider;
    private Camera _mainCamera;

    private bool _constructed = false;
    private IInteractable _currentInteractable;

    public void Construct(IInputProvider inputProvider, Camera mainCamera)
    {
        _inputProvider = inputProvider;
        _mainCamera = mainCamera;

        _constructed = true;
        Debug.Log($"{GetType().Name} consturcted with dependencies: {inputProvider.GetType().Name} | {mainCamera.GetType().Name}");
    }

    private void Update()
    {
        if (!_constructed)
            return;

        IInteractable foundInteractable = null;

        Ray ray = new(_mainCamera.transform.position, _mainCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, _interactionDistance, _interactionMask))
            raycastHit.collider.TryGetComponent<IInteractable>(out foundInteractable);

        if (_currentInteractable != foundInteractable)
        {
            _currentInteractable = foundInteractable;
            // Update UI here
        }

        if (_currentInteractable != null && _inputProvider.PlayerActions.Interact.WasPressedThisFrame())
            _currentInteractable.Interact();
    }

    private void OnDrawGizmosSelected()
    {
        Camera mainCamera = Camera.main;
        float distance = _interactionDistance;

        Ray ray = new(mainCamera.transform.position, mainCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, _interactionDistance, _interactionMask))
            distance = (raycastHit.point - mainCamera.transform.position).magnitude;

        Gizmos.color = Color.darkRed;
        Gizmos.DrawRay(mainCamera.transform.position, mainCamera.transform.forward * distance);
    }
}
