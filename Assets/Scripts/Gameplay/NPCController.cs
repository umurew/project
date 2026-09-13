using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EntityBlackboard))]
public class NPCController : MonoBehaviour
{
    [SerializeField] private BlackboardKey _destinationKey = new("Destination");
    [SerializeField] private BlackboardKey _isMovingKey = new("IsMoving");

    private Animator _animator;
    private NavMeshAgent _navMeshAgent;
    private EntityBlackboard _entityBlackboard;

    private readonly int _isMovingHash = Animator.StringToHash("IsMoving");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _entityBlackboard = GetComponent<EntityBlackboard>();
    }

    private void Update()
    {
        if (_entityBlackboard.TryGet(_destinationKey, out Vector3 destination))
            _navMeshAgent.SetDestination(destination);

        bool isMoving = _navMeshAgent.velocity.sqrMagnitude > 0.01f && !_navMeshAgent.isStopped;
        _entityBlackboard.Set(_isMovingKey, isMoving);
        _animator.SetBool(_isMovingHash, isMoving);
    }
}
