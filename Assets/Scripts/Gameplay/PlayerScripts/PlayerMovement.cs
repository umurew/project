using UnityEngine;

[RequireComponent(typeof(EntityBlackboard))]
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Entity Blackboard Keys")]
    [SerializeField] private BlackboardKey _canSprintKey = new("CanSprint");
    [SerializeField] private BlackboardKey _canJumpKey = new("CanJump");
    [SerializeField] private BlackboardKey _isMovingKey = new("IsMoving");

    [Header("Configuration")]
    [SerializeField] private float _walkSpeed = 5f;
    [SerializeField] private float _sprintMultiplier = 1.5f;
    [SerializeField] private float _jumpHeight = 3f;
    [SerializeField] private float _gravity = -9.81f;

    private IInputProvider _inputProvider;
    private Camera _mainCamera;
    private EntityBlackboard _entityBlackboard;
    private CharacterController _characterController;

    private float _verticalVector = 0f;
    private bool _isGrounded = false;
    private bool _constructed = false;

    public void Construct(IInputProvider inputProvider, Camera mainCamera)
    {
        _inputProvider = inputProvider;
        _mainCamera = mainCamera;

        _entityBlackboard = GetComponent<EntityBlackboard>();
        _characterController = GetComponent<CharacterController>();

        _constructed = true;
        Debug.Log($"{GetType().Name} consturcted with dependencies: {inputProvider.GetType().Name} | {mainCamera.GetType().Name}");
    }

    private void Update()
    {
        if (!_constructed)
            return;

        Vector3 cameraEulerAngles = _mainCamera.transform.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, cameraEulerAngles.y, 0f);

        Vector2 moveInput = _inputProvider.PlayerActions.Move.ReadValue<Vector2>();
        bool sprintInput = _inputProvider.PlayerActions.Sprint.IsPressed();
        bool jumpInput = _inputProvider.PlayerActions.Jump.WasPerformedThisFrame();

        Vector3 forwardVector = _mainCamera.transform.forward;
        forwardVector.y = 0f;
        forwardVector.Normalize();

        Vector3 rightVector = _mainCamera.transform.right;
        rightVector.y = 0f;
        rightVector.Normalize();

        Vector3 horizontalVector = forwardVector * moveInput.y + rightVector * moveInput.x;

        _isGrounded = _characterController.isGrounded;
        if (_isGrounded)
        {
            if (_verticalVector < 0f)
                _verticalVector = -2f;

            if (_entityBlackboard.TryGet(_canJumpKey, out bool canJump) && canJump && jumpInput)
            {
                _verticalVector = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
            }
        }
        else
            _verticalVector += _gravity * Time.deltaTime;

        float speed = (_isGrounded && _entityBlackboard.TryGet(_canSprintKey, out bool canSprint) && canSprint && sprintInput)
            ? _walkSpeed * _sprintMultiplier
            : _walkSpeed;

        Vector3 compositeVector = speed * horizontalVector + Vector3.up * _verticalVector;
        _characterController.Move(compositeVector * Time.deltaTime);

        bool isMoving = new Vector2(compositeVector.x, compositeVector.z).sqrMagnitude > 0.01f;
        _entityBlackboard.Set(_isMovingKey, isMoving);
    }
}
