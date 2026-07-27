using UnityEngine;
using UnityEngine.InputSystem;
using TouchPhase = UnityEngine.TouchPhase;

public class MovementPlayer : MonoBehaviour
{
    [SerializeField] private PlayerSettings settings;

    [Header("Visuals")]
    [SerializeField] private Animator animator;
    [SerializeField] private InputActionReference moveAction;
    public Transform characterModel; 

    private CharacterController controller;
    private Health health;
    private Vector3 moveDirection;
    private float verticalVelocity;
    
    private bool isDashing = false;
    private float dashTimer = 0f;
    private Vector3 currentDashDirection;
    
    void Awake()
    {
        CharacterHighlightLayer.Ensure(gameObject);

        settings = SettingsResolver.ResolvePlayerSettings(settings);

        if (settings == null)
        {
            Debug.LogWarning("[MovementPlayer] No PlayerSettings found (provider/inspector/resources).");
        }
    }

    void Start()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        controller = GetComponent<CharacterController>();
        health = GetComponent<Health>();

        if (health != null)
        {
            health.OnHit += HandleHit;
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnHit -= HandleHit;
        }
    }

    private void HandleHit(HitData hitData)
    {
        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }
    }

    // Update is called once per frame
    void Update()
    {
        /*if (isDashing)
        {
            HandleDash();
            return;
        }*/

        HandleMovementAndJump();
        //HandleDashInput();
    }

    public Vector3 GetInputDirection()
    {
        float moveX = 0f;
        float moveZ = 0f;

        if (moveAction != null && moveAction.action != null && moveAction.action.enabled)
        {
            Vector2 input = moveAction.action.ReadValue<Vector2>();
            
            if (input.magnitude > 0.05f)
            {
                moveX = input.x;
                moveZ = input.y;
            }
        }
        else
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveX = 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveX = -1f;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveZ = 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveZ = -1f;
            }

            if (Gamepad.current != null)
            {
                Vector2 stickInput = Gamepad.current.leftStick.ReadValue();
                if (stickInput.magnitude > 0.1f)
                {
                    moveX = stickInput.x;
                    moveZ = stickInput.y;
                }
            }
        }

        return new Vector3(moveX, 0f, moveZ).normalized;
    }

    private void HandleMovementAndJump()
    {
        if (controller == null || !controller.enabled)
        {
            return;
        }

        Vector3 inputDirection = GetInputDirection();
        
        if (!PlayerStateManager.Instance.CanPerformAction())
        {
            moveDirection = Vector3.zero;
            animator.SetFloat("Speed", 0f);
            if (!controller.isGrounded)
            {
                verticalVelocity -= settings.gravity * Time.deltaTime;
                moveDirection.y = verticalVelocity;
                controller.Move(moveDirection * Time.deltaTime);
            }
            return;
        }

        float moveX = inputDirection.x;
        float moveZ = inputDirection.z;

        // Optional: Run mechanic (can be activated)
        // bool isRunning = false;
        // if (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed) isRunning = true;
        // if (Gamepad.current != null && Gamepad.current.rightTrigger.isPressed) isRunning = true;
        // float currentSpeed = isRunning ? runSpeed : walkSpeed;
        float currentSpeed = settings.walkSpeed;
        
        moveDirection = inputDirection * currentSpeed;
        animator.SetFloat("Speed", inputDirection.magnitude);

        if (moveX != 0)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(moveX, 0, 0));
            characterModel.rotation = targetRotation;
        }

        if (controller.isGrounded)
        {
            verticalVelocity = -0.5f; 

            bool jumpPressed = false;
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) jumpPressed = true;
            if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) jumpPressed = true;

            if (jumpPressed)
            {
                verticalVelocity = settings.jumpForce;
            }
        }
        else
        {
            verticalVelocity -= settings.gravity * Time.deltaTime;
        }

        moveDirection.y = verticalVelocity;
        controller.Move(moveDirection * Time.deltaTime);
    }

    // Dash mechanic (optional, can be activated)

    /*private void HandleDashInput()
    {
        if (Keyboard.current.leftAltKey.wasPressedThisFrame && controller.isGrounded && !isDashing)
        {
            isDashing = true;
            dashTimer = dashDuration;
            
            float moveX = 0f; float moveZ = 0f;
            if (Keyboard.current.dKey.isPressed) moveX = 1f;
            if (Keyboard.current.aKey.isPressed) moveX = -1f;
            if (Keyboard.current.wKey.isPressed) moveZ = 1f;
            if (Keyboard.current.sKey.isPressed) moveZ = -1f;
            
            if (moveX == 0 && moveZ == 0)
            {
                currentDashDirection = characterModel.forward;
            }
            else
            {
                currentDashDirection = new Vector3(moveX, 0f, moveZ).normalized;
            }
        }
    }

    private void HandleDash()
    {
        dashTimer -= Time.deltaTime;
        Vector3 dashVelocity = currentDashDirection * dashSpeed;
        dashVelocity.y = -0.5f;
        
        controller.Move(dashVelocity * Time.deltaTime);

        if (dashTimer <= 0)
        {
            isDashing = false;
        }
    }*/
}
