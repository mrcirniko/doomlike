using UnityEngine;
using Doomlike.Player;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputReader))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;

    [Header("Movement")]
    public float moveSpeed = 8f;
    public float gravity = -20f;
    public float jumpHeight = 1.6f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 0.12f;
    public float minLookAngle = -80f;
    public float maxLookAngle = 80f;

    CharacterController controller;
    PlayerInputReader input;
    float verticalVelocity;
    float cameraPitch;

    Vector3 externalVelocity;
    bool externalVelocityActive;
    bool gravityEnabled = true;

    public CharacterController Controller => controller;
    public Transform CameraTransform => cameraTransform;
    public bool IsGrounded => controller.isGrounded;
    public Vector2 MoveInput => input.MoveInput;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<PlayerInputReader>();
    }

    void OnEnable()
    {
        if (input != null) input.JumpPressed += OnJumpPressed;
    }

    void OnDisable()
    {
        if (input != null) input.JumpPressed -= OnJumpPressed;
    }

    void OnJumpPressed()
    {
        if (!gravityEnabled) return;
        if (controller != null && controller.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight);
        }
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleCursor();
        Look();
        Move();
    }

    void HandleCursor()
    {
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (UnityEngine.InputSystem.Mouse.current != null &&
            UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame &&
            Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Move()
    {
        Vector3 velocity;

        if (externalVelocityActive)
        {
            velocity = externalVelocity;
        }
        else
        {
            Vector2 m = input.MoveInput;
            Vector3 wishDir = transform.right * m.x + transform.forward * m.y;
            wishDir = Vector3.ClampMagnitude(wishDir, 1f);
            velocity = wishDir * moveSpeed;
        }

        if (gravityEnabled)
        {
            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            verticalVelocity += gravity * Time.deltaTime;
            velocity.y = verticalVelocity;
        }
        else
        {
            verticalVelocity = 0f;
        }

        controller.Move(velocity * Time.deltaTime);
    }

    void Look()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        Vector2 look = input.LookInput;
        float mouseX = look.x * mouseSensitivity;
        float mouseY = look.y * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, minLookAngle, maxLookAngle);

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    public void SetExternalVelocity(Vector3 velocity, bool useGravity)
    {
        externalVelocity = velocity;
        externalVelocityActive = true;
        gravityEnabled = useGravity;
    }

    public void ClearExternalVelocity()
    {
        externalVelocityActive = false;
        gravityEnabled = true;
    }

    public void RotateYaw(float degrees)
    {
        transform.Rotate(Vector3.up * degrees);
    }
}
