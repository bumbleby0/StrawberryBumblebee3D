using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public enum CharacterType { Erishikgal, Fredrick, Ezikiel, Miranda }

    [Header("Character Selection")]
    public ErishikgalStats erishikgal;
    public FredrickStats fredrick;
    public EzikielStats ezikiel;
    public MirandaStats miranda;
    [SerializeField] private CharacterType activeCharacterType = CharacterType.Erishikgal;

    [Header("UI")]
    public UIController uiController; // Assign UIController in Inspector

    public float mouseSensitivity = 2f;
    public float groundCheckDistance = 0.2f;
    public LayerMask groundMask;

    public float slideSpeed = 15f;
    public float slideDuration = 0.7f;
    public float slideCameraHeight = 0.5f; // Height for camera during slide
    public float minSlideDuration = 0.5f;
    public float maxSlideDuration = 1.5f;
    public float minSlideSpeed = 10f;
    public float maxSlideSpeed = 25f;

    private Rigidbody rb;
    private float rotationY = 0f;
    private float cameraPitch = 0f;
    private Camera cam;
    private bool isGrounded;

    private bool isSliding = false;
    private float slideTimer = 0f;
    private Vector3 originalCameraLocalPos;
    private float currentSlideSpeed = 0f;
    private bool jumpQueued = false;

    void Start()
    {
        // Set active character from CharacterSelector
        if (!string.IsNullOrEmpty(CharacterSelector.SelectedCharacter))
        {
            switch (CharacterSelector.SelectedCharacter)
            {
                case "Fredrick":
                    activeCharacterType = CharacterType.Fredrick;
                    break;
                case "Ezikiel":
                    activeCharacterType = CharacterType.Ezikiel;
                    break;
                case "Erishikgal":
                    activeCharacterType = CharacterType.Erishikgal;
                    break;
                case "Miranda":
                    activeCharacterType = CharacterType.Miranda;
                    break;
            }
        }

        rb = GetComponent<Rigidbody>();
        cam = GetComponentInChildren<Camera>();
        Cursor.lockState = CursorLockMode.Locked;
        if (cam != null)
            originalCameraLocalPos = cam.transform.localPosition;

        SetHealthUI();
    }

    void Update()
    {
        // Character swap input
        if (Input.GetKeyDown(KeyCode.Alpha1)) SetActiveCharacter(CharacterType.Erishikgal);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetActiveCharacter(CharacterType.Fredrick);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetActiveCharacter(CharacterType.Ezikiel);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetActiveCharacter(CharacterType.Miranda);

        // Mouse look
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        rotationY += mouseX;
        transform.rotation = Quaternion.Euler(0, rotationY, 0);

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -80f, 80f);
        if (cam != null)
            cam.transform.localRotation = Quaternion.Euler(cameraPitch, 0, 0);

        // Ground check
        Collider col = GetComponent<Collider>();
        Vector3 rayOrigin = col.bounds.center;
        rayOrigin.y = col.bounds.min.y + 0.05f;
        isGrounded = Physics.Raycast(rayOrigin, Vector3.down, groundCheckDistance, groundMask);

        // Jump
        if (Input.GetButtonDown("Jump"))
        {
            if (isSliding)
            {
                // Queue jump for end of slide
                jumpQueued = true;
            }
            else if (isGrounded)
            {
                // Calculate intended horizontal velocity based on input and speed
                float moveX = Input.GetAxisRaw("Horizontal");
                float moveZ = Input.GetAxisRaw("Vertical");
                Vector3 moveInput = (transform.right * moveX + transform.forward * moveZ).normalized;
                bool canSprint = Input.GetKey(KeyCode.LeftShift);
                float currentSpeed = canSprint ? GetSprintSpeed() : GetMoveSpeed();

                Vector3 jumpVelocity;
                if (moveInput.sqrMagnitude > 0.01f)
                {
                    // Use intended movement direction and speed
                    Vector3 horizontal = moveInput * currentSpeed;
                    jumpVelocity = new Vector3(horizontal.x, GetJumpForce(), horizontal.z);
                }
                else
                {
                    // If no input, preserve current horizontal velocity
                    Vector3 velocity = rb.velocity;
                    jumpVelocity = new Vector3(velocity.x, GetJumpForce(), velocity.z);
                }
                rb.velocity = jumpVelocity;
            }
        }

        // Self-damage on "P" key
        if (Input.GetKeyDown(KeyCode.P))
        {
            SetCurrentHealth(Mathf.Max(0f, GetCurrentHealth() - 25f));
            SetHealthUI();
        }

        // Start slide
        if (Input.GetKeyDown(KeyCode.LeftControl) && isGrounded && !isSliding)
        {
            isSliding = true;

            // Use current horizontal momentum for slide speed
            Vector3 horizontalVelocity = rb.velocity;
            horizontalVelocity.y = 0;
            float entrySpeed = horizontalVelocity.magnitude;
            currentSlideSpeed = Mathf.Clamp(entrySpeed, minSlideSpeed, maxSlideSpeed);

            // Slide duration scales with entry speed
            slideTimer = Mathf.Lerp(minSlideDuration, maxSlideDuration, (entrySpeed - minSlideSpeed) / (maxSlideSpeed - minSlideSpeed));

            if (cam != null)
            {
                Vector3 lowered = originalCameraLocalPos;
                lowered.y = slideCameraHeight;
                cam.transform.localPosition = lowered;
            }
        }

        // End slide
        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0f)
            {
                isSliding = false;
                if (cam != null)
                    cam.transform.localPosition = originalCameraLocalPos;

                // Perform jump if queued
                if (jumpQueued && isGrounded)
                {
                    float moveX = Input.GetAxisRaw("Horizontal");
                    float moveZ = Input.GetAxisRaw("Vertical");
                    Vector3 moveInput = (transform.right * moveX + transform.forward * moveZ).normalized;
                    bool canSprint = Input.GetKey(KeyCode.LeftShift);
                    float currentSpeed = canSprint ? GetSprintSpeed() : GetMoveSpeed();

                    Vector3 jumpVelocity;
                    if (moveInput.sqrMagnitude > 0.01f)
                    {
                        Vector3 horizontal = moveInput * currentSpeed;
                        jumpVelocity = new Vector3(horizontal.x, GetJumpForce(), horizontal.z);
                    }
                    else
                    {
                        Vector3 velocity = rb.velocity;
                        jumpVelocity = new Vector3(velocity.x, GetJumpForce(), velocity.z);
                    }
                    rb.velocity = jumpVelocity;
                }
                jumpQueued = false;
            }
        }

        // Update health bar every frame (optional, if health can change elsewhere)
        if (uiController != null)
        {
            SetHealthUI();
        }
    }

    void FixedUpdate()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");
        
        Vector3 moveInput = (transform.right * moveX + transform.forward * moveZ).normalized;

        bool canSprint = isGrounded && Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = isSliding ? currentSlideSpeed : (canSprint ? GetSprintSpeed() : GetMoveSpeed());

        Vector3 velocity = rb.velocity;

        if (isGrounded || isSliding)
        {
            // Use MovePosition for smooth, physics-friendly ground movement
            if (moveInput.sqrMagnitude > 0.01f)
            {
                Vector3 move = moveInput * currentSpeed * Time.fixedDeltaTime;
                rb.MovePosition(rb.position + new Vector3(move.x, 0, move.z));
            }
            // else: do nothing, let drag/friction stop the player naturally
        }
        else
        {
            // Air movement: allow strong air control
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
            Vector3 desiredHorizontalVelocity = moveInput * GetMoveSpeed();
            float airControl = 1.0f; // 1.0f = instant, <1.0f = more floaty
            Vector3 newHorizontalVelocity = Vector3.Lerp(horizontalVelocity, desiredHorizontalVelocity, airControl * Time.fixedDeltaTime);

            // Optional: Clamp to max ground speed
            float maxAirSpeed = GetMoveSpeed();
            if (newHorizontalVelocity.magnitude > maxAirSpeed)
                newHorizontalVelocity = newHorizontalVelocity.normalized * maxAirSpeed;

            rb.velocity = new Vector3(newHorizontalVelocity.x, velocity.y, newHorizontalVelocity.z);
        }
    }

    // --- Character Swapping Helpers ---

    void SetActiveCharacter(CharacterType type)
    {
        activeCharacterType = type;
        SetHealthUI();
    }

    void SetHealthUI()
    {
        if (uiController == null) return;
        uiController.SetHealth(GetCurrentHealth(), GetMaxHealth());
    }

    float GetCurrentHealth()
    {
        switch (activeCharacterType)
        {
            case CharacterType.Erishikgal: return erishikgal != null ? erishikgal.currentHealth : 0f;
            case CharacterType.Fredrick: return fredrick != null ? fredrick.currentHealth : 0f;
            case CharacterType.Ezikiel: return ezikiel != null ? ezikiel.currentHealth : 0f;
            case CharacterType.Miranda: return miranda != null ? miranda.currentHealth : 0f;
            default: return 0f;
        }
    }

    void SetCurrentHealth(float value)
    {
        switch (activeCharacterType)
        {
            case CharacterType.Erishikgal: if (erishikgal != null) erishikgal.currentHealth = value; break;
            case CharacterType.Fredrick: if (fredrick != null) fredrick.currentHealth = value; break;
            case CharacterType.Ezikiel: if (ezikiel != null) ezikiel.currentHealth = value; break;
            case CharacterType.Miranda: if (miranda != null) miranda.currentHealth = value; break;
        }
    }

    float GetMaxHealth()
    {
        switch (activeCharacterType)
        {
            case CharacterType.Erishikgal: return erishikgal != null ? erishikgal.maxHealth : 0f;
            case CharacterType.Fredrick: return fredrick != null ? fredrick.maxHealth : 0f;
            case CharacterType.Ezikiel: return ezikiel != null ? ezikiel.maxHealth : 0f;
            case CharacterType.Miranda: return miranda != null ? miranda.maxHealth : 0f;
            default: return 0f;
        }
    }

    float GetJumpForce()
    {
        switch (activeCharacterType)
        {
            case CharacterType.Erishikgal: return erishikgal != null ? erishikgal.jumpForce : 0f;
            case CharacterType.Fredrick: return fredrick != null ? fredrick.jumpForce : 0f;
            case CharacterType.Ezikiel: return ezikiel != null ? ezikiel.jumpForce : 0f;
            case CharacterType.Miranda: return miranda != null ? miranda.jumpForce : 0f;
            default: return 0f;
        }
    }

    float GetMoveSpeed()
    {
        switch (activeCharacterType)
        {
            case CharacterType.Erishikgal: return erishikgal != null ? erishikgal.moveSpeed : 0f;
            case CharacterType.Fredrick: return fredrick != null ? fredrick.moveSpeed : 0f;
            case CharacterType.Ezikiel: return ezikiel != null ? ezikiel.moveSpeed : 0f;
            case CharacterType.Miranda: return miranda != null ? miranda.moveSpeed : 0f;
            default: return 0f;
        }
    }

    float GetSprintSpeed()
    {
        switch (activeCharacterType)
        {
            case CharacterType.Erishikgal: return erishikgal != null ? erishikgal.sprintSpeed : 0f;
            case CharacterType.Fredrick: return fredrick != null ? fredrick.sprintSpeed : 0f;
            case CharacterType.Ezikiel: return ezikiel != null ? ezikiel.sprintSpeed : 0f;
            case CharacterType.Miranda: return miranda != null ? miranda.sprintSpeed : 0f;
            default: return 0f;
        }
    }

    Rigidbody GetRigidbody()
    {
        return rb;
    }
}
