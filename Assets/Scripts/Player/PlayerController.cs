using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// First-person player controller with movement, sprinting, crouching (stealth), and mouse look.
/// No weapons - survival through movement and stealth only.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 4f;
    public float sprintSpeed = 7f;
    public float crouchSpeed = 2f;
    public float gravity = -20f;
    public float jumpHeight = 1.2f;
    public float turnSpeed = 12f;

    [Header("Mouse Look")]
    public bool useMouseLook = true;
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 80f;

    [Header("Crouch")]
    public float standHeight = 2f;
    public float crouchHeight = 1f;
    public float crouchTransitionSpeed = 8f;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 20f;
    public float staminaRecoveryRate = 15f;
    public float wallHitStaminaDrainRate = 10f;

    [Header("References")]
    public Transform cameraHolder;
    public Transform movementReference;

    private CharacterController controller;
    private float verticalVelocity;
    private float cameraPitch;
    private float currentStamina;
    private bool isCrouching;
    private bool isSprinting;
    private bool isPushingIntoWall;
    private bool isTryingToMoveHorizontally;
    private bool staminaDepletedGameOver;
    private float targetHeight;

    public bool IsCrouching => isCrouching;
    public bool IsSprinting => isSprinting;
    public float CurrentStamina => currentStamina;
    public float StaminaPercent => currentStamina / maxStamina;
    public Vector3 Velocity => controller != null ? controller.velocity : Vector3.zero;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
            controller = gameObject.AddComponent<CharacterController>();

        controller.height = standHeight;
        controller.center = Vector3.up * (standHeight / 2f);
        targetHeight = standHeight;
        currentStamina = maxStamina;

        if (cameraHolder != null)
        {
            cameraHolder.localPosition = new Vector3(0f, standHeight * 0.9f, 0f);
            cameraHolder.localRotation = Quaternion.identity;
        }

        Cursor.lockState = useMouseLook ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !useMouseLook;
    }

    void Update()
    {
        HandleLook();
        HandleMovement();
        HandleCrouch();
        HandleStamina();
    }

    void LateUpdate()
    {
        UpdateCameraHolder();
    }

    void HandleLook()
    {
        if (!useMouseLook) return;
        if (Mouse.current == null) return;

        Vector2 lookDelta = Mouse.current.delta.ReadValue() * mouseSensitivity * 0.12f;
        transform.Rotate(Vector3.up, lookDelta.x);

        cameraPitch = Mathf.Clamp(cameraPitch - lookDelta.y, -maxLookAngle, maxLookAngle);
    }

    void UpdateCameraHolder()
    {
        if (cameraHolder == null) return;

        float cameraHeight = Mathf.Lerp(crouchHeight, standHeight, 1f - (controller.height - crouchHeight) / Mathf.Max(0.01f, standHeight - crouchHeight));
        cameraHolder.localPosition = new Vector3(0f, Mathf.Max(0.8f, controller.height * 0.9f), 0f);
        cameraHolder.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    void HandleMovement()
    {
        if (Keyboard.current == null) return;
        float moveX = 0f, moveZ = 0f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveX -= 1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveX += 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveZ -= 1f;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveZ += 1f;

        isSprinting = Keyboard.current.leftShiftKey.isPressed && !isCrouching && currentStamina > 0f;

        float speed = isCrouching ? crouchSpeed : (isSprinting ? sprintSpeed : walkSpeed);

        Transform moveBasis = movementReference != null ? movementReference : transform;
        Vector3 flatForward = Vector3.ProjectOnPlane(moveBasis.forward, Vector3.up).normalized;
        Vector3 flatRight = Vector3.ProjectOnPlane(moveBasis.right, Vector3.up).normalized;
        Vector3 move = (flatRight * moveX + flatForward * moveZ);
        move = Vector3.ClampMagnitude(move, 1f) * speed;
        isTryingToMoveHorizontally = new Vector2(moveX, moveZ).sqrMagnitude > 0.01f;

        if (controller.isGrounded)
        {
            verticalVelocity = -2f;
            if (Keyboard.current.spaceKey.wasPressedThisFrame && !isCrouching)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }

        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity;

        Vector3 preMovePosition = transform.position;
        Vector3 desiredStep = move * Time.deltaTime;
        float desiredHorizontalDistance = new Vector2(desiredStep.x, desiredStep.z).magnitude;

        CollisionFlags collisionFlags = controller.Move(desiredStep);

        // Treat movement as blocked when intended horizontal step is significantly larger than actual step.
        Vector3 actualStep = transform.position - preMovePosition;
        float actualHorizontalDistance = new Vector2(actualStep.x, actualStep.z).magnitude;
        bool sideCollision = (collisionFlags & CollisionFlags.Sides) != 0;
        bool blockedByWall = desiredHorizontalDistance > 0.01f && actualHorizontalDistance < desiredHorizontalDistance * 0.6f;

        bool blockedByProbe = false;
        if (isTryingToMoveHorizontally)
        {
            Vector3 desiredDir = new Vector3(moveX, 0f, moveZ).normalized;
            Vector3 origin = transform.position + Vector3.up * (controller.height * 0.5f);
            float probeDistance = controller.radius + 0.2f;
            blockedByProbe = Physics.SphereCast(origin, controller.radius * 0.85f, desiredDir,
                out _, probeDistance, ~0, QueryTriggerInteraction.Ignore);
        }

        isPushingIntoWall = isTryingToMoveHorizontally && (sideCollision || blockedByWall || blockedByProbe);
    }

    void HandleCrouch()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current.cKey.wasPressedThisFrame || Keyboard.current.leftCtrlKey.wasPressedThisFrame)
        {
            isCrouching = !isCrouching;
            targetHeight = isCrouching ? crouchHeight : standHeight;
        }

        float currentHeight = controller.height;
        if (!Mathf.Approximately(currentHeight, targetHeight))
        {
            float newHeight = Mathf.Lerp(currentHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);
            controller.height = newHeight;
            controller.center = Vector3.up * (newHeight / 2f);
        }
    }

    void HandleStamina()
    {
        // No passive stamina regeneration.
        if (isSprinting)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
            currentStamina = Mathf.Max(0f, currentStamina);
        }

        if (isPushingIntoWall)
        {
            currentStamina -= wallHitStaminaDrainRate * Time.deltaTime;
            currentStamina = Mathf.Max(0f, currentStamina);
        }

        if (!staminaDepletedGameOver && currentStamina <= 0f)
        {
            staminaDepletedGameOver = true;
            GameManager.Instance?.OnPlayerDied();
        }
    }

    public float GetNoiseLevel()
    {
        if (!controller.isGrounded) return 0f;
        float speed = new Vector3(controller.velocity.x, 0, controller.velocity.z).magnitude;
        if (speed < 0.1f) return 0f;
        if (isCrouching) return 0.2f;
        if (isSprinting) return 1f;
        return 0.5f;
    }
}
