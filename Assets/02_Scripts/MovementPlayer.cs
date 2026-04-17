using UnityEngine;
using UnityEngine.InputSystem;

public class MovementPlayer : MonoBehaviour
{
    [Header("Movement (X/Z-Achse)")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    
    [Header("Jump (Y-Achse)")]
    public float jumpForce = 8f;
    public float gravity = 20f;
    
    [Header("Future maybe Dash")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public KeyCode dashKey = KeyCode.LeftAlt;

    [Header("Visuals")]
    public Transform characterModel; 

    private CharacterController controller;
    private Vector3 moveDirection;
    private float verticalVelocity;
    
    private bool isDashing = false;
    private float dashTimer = 0f;
    private Vector3 currentDashDirection;
    
    void Start()
    {
        controller = GetComponent<CharacterController>();
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

    private void HandleMovementAndJump()
    {
        float moveX = 0f;
        float moveZ = 0f;
        
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveX = 1f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveX = -1f;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveZ = 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveZ = -1f;

        if (Gamepad.current != null)
        {
            Vector2 stickInput = Gamepad.current.leftStick.ReadValue();
            Vector3 dpadInput = Gamepad.current.dpad.ReadValue();

            if (stickInput.magnitude > 0.1f)
            {
                moveX = stickInput.x;
                moveZ = stickInput.y;
            }
            else if (dpadInput.magnitude > 0.1f)
            {
                moveX = dpadInput.x;
                moveZ = dpadInput.y;
            }
        }

        Vector3 inputDirection = new Vector3(moveX, 0f, moveZ).normalized;

        // Optional: Run mechanic (can be activated)
        // bool isRunning = false;
        // if (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed) isRunning = true;
        // if (Gamepad.current != null && Gamepad.current.rightTrigger.isPressed) isRunning = true;
        // float currentSpeed = isRunning ? runSpeed : walkSpeed;
        float currentSpeed = walkSpeed;
        
        moveDirection = inputDirection * currentSpeed;

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
                verticalVelocity = jumpForce;
            }
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
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
