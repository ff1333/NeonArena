using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField, Min(0.5f)] private float moveSpeed = 6f;
    [SerializeField, Min(0.5f)] private float maximumMoveSpeed = 9f;
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private ArenaBounds arenaBounds;
    [SerializeField, Min(0f)] private float edgePadding = 0.45f;
    [SerializeField, Range(0f, 0.5f)] private float inputDeadZone = 0.08f;
    [SerializeField, Min(1f)] private float acceleration = 14f;
    [SerializeField, Min(1f)] private float deceleration = 22f;

    private Rigidbody2D body;
    private InputAction moveAction;
    private Vector2 targetInput;
    private Vector2 movementInput;

    public float MoveSpeed => moveSpeed;
    public float MaximumMoveSpeed => maximumMoveSpeed;
    public bool CanIncreaseMoveSpeed =>
        moveSpeed < maximumMoveSpeed - 0.001f;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        maximumMoveSpeed = Mathf.Max(0.5f, maximumMoveSpeed);
        moveSpeed = Mathf.Clamp(moveSpeed, 0.5f, maximumMoveSpeed);
        inputDeadZone = Mathf.Clamp(inputDeadZone, 0f, 0.5f);
        acceleration = Mathf.Max(1f, acceleration);
        deceleration = Mathf.Max(acceleration, deceleration);

        if (inputActions == null)
        {
            Debug.LogError("PlayerMovement requires an Input Action Asset.", this);
            return;
        }

        if (arenaBounds == null)
        {
            Debug.LogError("PlayerMovement requires ArenaBounds.", this);
            return;
        }

        moveAction = inputActions.FindAction("Player/Move", true);
    }

    private void OnEnable()
    {
        moveAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        targetInput = Vector2.zero;
        movementInput = Vector2.zero;
    }

    private void Update()
    {
        if (moveAction == null)
        {
            return;
        }

        targetInput = ApplyDeadZone(
            moveAction.ReadValue<Vector2>(),
            inputDeadZone);
        float response = targetInput.sqrMagnitude > movementInput.sqrMagnitude
            ? acceleration
            : deceleration;
        movementInput = Vector2.MoveTowards(
            movementInput,
            targetInput,
            response * Time.deltaTime);
    }

    private void FixedUpdate()
    {
        Vector2 nextPosition =
            body.position + movementInput * moveSpeed * Time.fixedDeltaTime;

        if (arenaBounds != null)
        {
            nextPosition = arenaBounds.ClampPoint(nextPosition, edgePadding);
        }

        body.MovePosition(nextPosition);
    }

    public static Vector2 ApplyDeadZone(Vector2 value, float deadZone)
    {
        value = Vector2.ClampMagnitude(value, 1f);
        float magnitude = value.magnitude;
        deadZone = Mathf.Clamp(deadZone, 0f, 0.99f);

        if (magnitude <= deadZone)
        {
            return Vector2.zero;
        }

        float adjustedMagnitude =
            (magnitude - deadZone) / (1f - deadZone);
        return value / magnitude * adjustedMagnitude;
    }

    public void AddMoveSpeed(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        moveSpeed = Mathf.Min(maximumMoveSpeed, moveSpeed + amount);
    }
}
