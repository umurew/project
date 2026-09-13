using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(EntityBlackboard))]
public class InteractionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PanelRenderer _interactionRenderer;

    [Header("Configuration")]
    [SerializeField] private float _interactionDistance = 2.5f;
    [SerializeField] private LayerMask _interactionMask;

    private IInputProvider _inputProvider;
    private Camera _mainCamera;

    private bool _constructed = false;
    private IInteractable _currentInteractable;
    private VisualElement _container;
    private VisualElement _keyContainer;
    private VisualElement _floatingKeyContainer;
    private Label _descriptionLabel;
    private int _panelVersion = 0;

    public void Construct(IInputProvider inputProvider, Camera mainCamera)
    {
        _inputProvider = inputProvider;
        _mainCamera = mainCamera;

        _constructed = true;
        Debug.Log($"{GetType().Name} consturcted with dependencies: {inputProvider.GetType().Name} | {mainCamera.GetType().Name}");
    }

    private void OnEnable()
    {
        if (_interactionRenderer != null)
            _interactionRenderer.RegisterUIReloadCallback(OnUIReload);
    }

    private void OnDisable()
    {
        if (_interactionRenderer != null)
            _interactionRenderer.UnregisterUIReloadCallback(OnUIReload);
    }

    private void OnUIReload(PanelRenderer panelRenderer, VisualElement rootElement, int version)
    {
        if (version == _panelVersion) return;
        _panelVersion = version;

        if (rootElement != null)
        {
            _container = rootElement.Q<VisualElement>("container");
            _keyContainer = rootElement.Q<VisualElement>("key-container");
            _floatingKeyContainer = rootElement.Q<VisualElement>("floating-key-container");
            _descriptionLabel = rootElement.Q<Label>("description");

            if (_container != null)
                _container.style.display = DisplayStyle.Flex;
        }
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
            UpdateUI();
        }
        else if (_currentInteractable != null)
        {
            if (_descriptionLabel != null)
                _descriptionLabel.text = _currentInteractable.Description();
        }

        if (_currentInteractable != null && _inputProvider.PlayerActions.Interact.WasPressedThisFrame())
        {
            _currentInteractable.Interact();
            TriggerKeyBounce(_keyContainer);
            TriggerKeyBounce(_floatingKeyContainer);
        }

        UpdateFloatingUI();
    }

    private void UpdateUI()
    {
        if (_container == null) return;

        if (_currentInteractable != null)
        {
            _container.AddToClassList("visible");
            _descriptionLabel.text = _currentInteractable.Description();
        }
        else
            _container.RemoveFromClassList("visible");
    }

    private void TriggerKeyBounce(VisualElement visualElement)
    {
        if (visualElement == null) return;

        visualElement.AddToClassList("pressed");
        visualElement.schedule.Execute(() => visualElement.RemoveFromClassList("pressed")).StartingIn(100);
    }

    private void UpdateFloatingUI()
    {
        if (_floatingKeyContainer == null) return;

        if (_currentInteractable != null)
        {
            Transform anchor = _currentInteractable.GetUIAnchorPosition();
            Vector3 worldPosition = anchor != null ? anchor.position : ((MonoBehaviour)_currentInteractable).transform.position;
            Vector3 viewportPos = _mainCamera.WorldToViewportPoint(worldPosition);
            bool isBehindCamera = viewportPos.z < 0;

            if (isBehindCamera)
                _floatingKeyContainer.RemoveFromClassList("visible");
            else
            {
                Vector2 panelPosition = RuntimePanelUtils.CameraTransformWorldToPanel(
                    _floatingKeyContainer.panel,
                    worldPosition,
                    _mainCamera
                );

                _floatingKeyContainer.style.left = panelPosition.x;
                _floatingKeyContainer.style.top = panelPosition.y;
                _floatingKeyContainer.AddToClassList("visible");
            }
        }
        else
            _floatingKeyContainer.RemoveFromClassList("visible");
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
