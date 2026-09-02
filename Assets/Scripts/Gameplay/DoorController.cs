using Cysharp.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(EntityBlackboard))]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Collider))]
public class DoorController : MonoBehaviour, IInteractable
{
    [Header("Entity Blackboard Keys")]
    [SerializeField] private BlackboardKey _isLockedKey = new("IsLocked");
    [SerializeField] private BlackboardKey _isOpenedKey = new("IsOpened");

    [Header("Configuration")]
    [SerializeField] private float _speed = 10f;
    [SerializeField] private float _hingeAngle = 90f;
    [SerializeField] private bool _isInwards = true;
    [SerializeField] private bool _respectDebounce = true;

    [Header("References")]
    [SerializeField] private AudioClip _doorLockClip;
    [SerializeField] private AudioClip _doorOpenClip;
    [SerializeField] private AudioClip _doorCloseClip;

    private EntityBlackboard _entityBlackboard;
    private AudioSource _audioSource;

    private Quaternion _defaultRotation;
    private Quaternion _targetRotation;
    private bool _inDebounce;

    private void Awake()
    {
        _entityBlackboard = GetComponent<EntityBlackboard>();
        _audioSource = GetComponent<AudioSource>();

        _defaultRotation = transform.localRotation;
        _targetRotation = _defaultRotation;
        _inDebounce = false;
    }

    private void Start()
    {
        _entityBlackboard.RegisterCallback(_isOpenedKey, OnOpenStateChanged);

        if (_entityBlackboard.TryGet(_isOpenedKey, out bool isOpened))
            UpdateTargetRotation(isOpened);
    }

    private void Update() => transform.localRotation = Quaternion.RotateTowards(transform.localRotation, _targetRotation, _speed * Time.deltaTime);

    private async UniTaskVoid HandleDebounce()
    {
        _inDebounce = true;

        while (transform.localRotation != _targetRotation)
            await UniTask.Yield();

        _inDebounce = false;
    }

    private void UpdateTargetRotation(bool isOpened)
    {
        float targetAngle = isOpened ? _hingeAngle : 0f;

        if (_isInwards)
            targetAngle = -targetAngle;

        _targetRotation = _defaultRotation * Quaternion.Euler(0f, targetAngle, 0f);
    }

    private void OnOpenStateChanged()
    {
        if (_entityBlackboard.TryGet(_isOpenedKey, out bool isOpened))
        {
            UpdateTargetRotation(isOpened);
            _audioSource.PlayOneShot(isOpened ? _doorOpenClip : _doorCloseClip);
        }
    }

    private void OnDestroy() => _entityBlackboard.UnregisterCallback(_isOpenedKey, OnOpenStateChanged);

    private void OnDrawGizmosSelected()
    {
        Collider collider = GetComponent<Collider>();

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);

        Vector3 position = transform.position;

        Gizmos.color = Color.blue;
        Vector3 axisStart = position - transform.up * 0.25f;
        Vector3 axisEnd = position + transform.up * 2.5f;
        Gizmos.DrawLine(axisStart, axisEnd);

        Gizmos.color = Color.darkRed;
        Vector3 arrowBase = position + transform.right * 1.125f + transform.up * 1.125f;
        Vector3 arrowTip = arrowBase + transform.forward * 0.2f;

        Gizmos.DrawLine(arrowBase - transform.forward * 0.2f, arrowBase + transform.forward * 0.2f);

        Vector3 leftFlank = arrowTip - transform.right * 0.1f - transform.forward * 0.1f;
        Gizmos.DrawLine(leftFlank, arrowTip);

        Vector3 rightFlank = arrowTip + transform.right * 0.1f - transform.forward * 0.1f;
        Gizmos.DrawLine(rightFlank, arrowTip);
    }

    public void Interact()
    {
        if (_respectDebounce && _inDebounce)
            return;

        if (_entityBlackboard.TryGet(_isLockedKey, out bool isLocked) && isLocked)
        {
            _audioSource.PlayOneShot(_doorLockClip);
            return;
        }

        if (_entityBlackboard.TryGet(_isOpenedKey, out bool isOpened))
        {
            bool newState = !isOpened;
            _entityBlackboard.Set(_isOpenedKey, newState);
        }

        if (_respectDebounce)
            HandleDebounce().Forget();
    }
}