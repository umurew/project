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
    [SerializeField] private float _speed = 2f;
    [SerializeField] private float _hingeAngle = 90f;
    [SerializeField] private bool _isInwards = true;
    [SerializeField] private bool _respectDebounce = true;

    [Header("References")]
    [SerializeField] private AudioClip _doorLockClip;
    [SerializeField] private AudioClip _doorOpenClip;
    [SerializeField] private AudioClip _doorCloseClip;

    private EntityBlackboard _entityBlackboard;
    private AudioSource _audioSource;

    private bool _inDebounce;
    private Quaternion _closedRotation;
    private Quaternion _openRotation;
    private Quaternion _targetRotation;

    private void Awake()
    {
        _entityBlackboard = GetComponent<EntityBlackboard>();
        _audioSource = GetComponent<AudioSource>();

        _inDebounce = false;

        float angle = _isInwards ? -_hingeAngle : _hingeAngle;
        _closedRotation = transform.localRotation;
        _openRotation = _closedRotation * Quaternion.Euler(0, angle, 0);

        _targetRotation = _closedRotation;
    }

    private void Start()
    {
        _entityBlackboard.RegisterCallback(_isOpenedKey, OnOpenStateChanged);

        if (_entityBlackboard.TryGet(_isOpenedKey, out bool isOpened))
            _targetRotation = isOpened ? _openRotation : _closedRotation;
    }

    private void Update()
    {
        if (transform.localRotation == _targetRotation)
            return;

        transform.localRotation = Quaternion.RotateTowards(transform.localRotation, _targetRotation, _speed * Time.deltaTime);
    }

    private async UniTaskVoid HandleDebounce()
    {
        _inDebounce = true;

        await UniTask.WaitUntil(() => Quaternion.Angle(transform.localRotation, _targetRotation) < 0.5f, cancellationToken: this.GetCancellationTokenOnDestroy());
        transform.localRotation = _targetRotation;

        _inDebounce = false;
    }

    private void OnOpenStateChanged()
    {
        if (_entityBlackboard.TryGet(_isOpenedKey, out bool isOpened))
        {
            _targetRotation = isOpened ? _openRotation : _closedRotation;
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