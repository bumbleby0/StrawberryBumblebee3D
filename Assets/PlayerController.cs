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

    [Header("Attack")]
    public LayerMask attackMask = ~0; // layers hittable by attacks
    public float meleeRange = 2f;
    public float meleeRadius = 0.75f;
    public float rangedMaxDistance = 100f;

    private enum AttackMode { Melee, Ranged }
    private AttackMode currentAttackMode = AttackMode.Melee;

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

    // Erishikgal Inferno mode (hold E to activate while charge > 1)
    private bool isInfernoActive = false;

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
                // account for Erishikgal's Inferno doubling
                if (isInfernoActive && activeCharacterType == CharacterType.Erishikgal)
                    currentSpeed *= 2f;

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
            // Show inferno bar only when Erishikgal is the active character
            if (activeCharacterType == CharacterType.Erishikgal && erishikgal != null)
            {
                uiController.ShowInferno(true);
                uiController.SetInferno(erishikgal.InfernoCharge, erishikgal.MaxInfernoCharge);
            }
            else
            {
                uiController.ShowInferno(false);
            }
        }

        // Handle attack input: Left click to attack, R to toggle melee/ranged (Fredrick cannot toggle)
        if (Input.GetKeyDown(KeyCode.R))
        {
            ToggleAttackMode();
        }
        if (Input.GetMouseButtonDown(0))
        {
            if (currentAttackMode == AttackMode.Melee) DoMeleeAttack(); else DoRangedAttack();
        }

        // Erishikgal Inferno input: hold E to activate while Erishikgal and has charge > 1
        if (activeCharacterType == CharacterType.Erishikgal && erishikgal != null)
        {
            // Debug info to verify values and input
            Debug.Log($"[Inferno Debug] Charge={erishikgal.InfernoCharge} Max={erishikgal.MaxInfernoCharge} isInfernoActive={isInfernoActive} EDown={Input.GetKey(KeyCode.E)}");
            bool wantInferno = Input.GetKey(KeyCode.E) && erishikgal.InfernoCharge > 1f;
            if (wantInferno && !isInfernoActive)
            {
                isInfernoActive = true;
                Debug.Log("Erishikgal Inferno activated");
            }
            else if (!wantInferno && isInfernoActive)
            {
                isInfernoActive = false;
                Debug.Log("Erishikgal Inferno deactivated");
            }

            // If active, drain over time here (use Time.deltaTime for visible UI update)
            if (isInfernoActive)
            {
                float drainPerSecond = (erishikgal.MaxInfernoCharge > 0f) ? (erishikgal.MaxInfernoCharge / 8f) : (100f / 8f);
                float delta = drainPerSecond * Time.deltaTime;
                float before = erishikgal.InfernoCharge;
                erishikgal.InfernoCharge = Mathf.Max(0f, erishikgal.InfernoCharge - delta);
                Debug.Log($"[Inferno Debug] Draining {delta} (before={before} after={erishikgal.InfernoCharge})");

                if (erishikgal.InfernoCharge <= 0f)
                {
                    isInfernoActive = false;
                    Debug.Log("Erishikgal Inferno depleted");
                }

                // update UI immediately
                if (uiController != null)
                    uiController.SetInferno(erishikgal.InfernoCharge, erishikgal.MaxInfernoCharge);
            }
            else
            {
                // Passive recharge when not using Inferno
                if (erishikgal.InfernoCharge < erishikgal.MaxInfernoCharge && erishikgal.InfernoRechargeRatePerSecond > 0f)
                {
                    float recharge = erishikgal.InfernoRechargeRatePerSecond * Time.deltaTime;
                    float before = erishikgal.InfernoCharge;
                    erishikgal.InfernoCharge = Mathf.Min(erishikgal.MaxInfernoCharge, erishikgal.InfernoCharge + recharge);
                    Debug.Log($"[Inferno Debug] Recharging {recharge} (before={before} after={erishikgal.InfernoCharge})");
                    if (uiController != null)
                        uiController.SetInferno(erishikgal.InfernoCharge, erishikgal.MaxInfernoCharge);
                }
            }
        }
    }

    void FixedUpdate()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");
        
        Vector3 moveInput = (transform.right * moveX + transform.forward * moveZ).normalized;

        bool canSprint = isGrounded && Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = isSliding ? currentSlideSpeed : (canSprint ? GetSprintSpeed() : GetMoveSpeed());
        if (isInfernoActive && activeCharacterType == CharacterType.Erishikgal)
            currentSpeed *= 2f;

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
            // respect inferno when calculating desired air velocity
            float airMaxSpeed = GetMoveSpeed();
            if (isInfernoActive && activeCharacterType == CharacterType.Erishikgal)
                airMaxSpeed *= 2f;
            Vector3 desiredHorizontalVelocity = moveInput * airMaxSpeed;
            float airControl = 1.0f; // 1.0f = instant, <1.0f = more floaty
            Vector3 newHorizontalVelocity = Vector3.Lerp(horizontalVelocity, desiredHorizontalVelocity, airControl * Time.fixedDeltaTime);

            // Optional: Clamp to max ground speed
            if (newHorizontalVelocity.magnitude > airMaxSpeed)
                newHorizontalVelocity = newHorizontalVelocity.normalized * airMaxSpeed;

            rb.velocity = new Vector3(newHorizontalVelocity.x, velocity.y, newHorizontalVelocity.z);
        }

        // Prevent small bounce on landing: if grounded and moving downward, zero vertical velocity
        if (isGrounded && rb.velocity.y < 0f)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
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

    // --- Attack helpers ---
    void ToggleAttackMode()
    {
        if (activeCharacterType == CharacterType.Fredrick)
        {
            Debug.Log("Fredrick cannot toggle to ranged attacks.");
            return;
        }
        currentAttackMode = currentAttackMode == AttackMode.Melee ? AttackMode.Ranged : AttackMode.Melee;
        Debug.Log($"Switched attack mode to: {currentAttackMode}");
    }

    void DoMeleeAttack()
    {
        float damage = GetMeleeDamage();
        Vector3 center = transform.position + transform.forward * (meleeRange * 0.5f) + Vector3.up * 1f;
        Collider[] hits = Physics.OverlapSphere(center, meleeRadius, attackMask);
        foreach (var col in hits)
        {
            if (col == null) continue;
            if (col.transform.IsChildOf(transform) || col.gameObject == gameObject) continue;
            col.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        }
        Debug.Log($"Melee attack dealt {damage} to {hits.Length} colliders");
        #if UNITY_EDITOR
        Debug.DrawLine(transform.position + Vector3.up * 1f, center, Color.red, 0.5f);
        #endif
    }

    void DoRangedAttack()
    {
        float damage = GetRangedDamage();
        Ray ray = (cam != null) ? new Ray(cam.transform.position, cam.transform.forward) : new Ray(transform.position + Vector3.up * 1f, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, rangedMaxDistance, attackMask))
        {
            hit.collider.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            Debug.Log($"Ranged attack hit {hit.collider.name} for {damage}");
        }
        else
        {
            Debug.Log($"Ranged attack missed (dealt {damage} to nothing)");
        }
    }

    float GetMeleeDamage()
    {
        switch (activeCharacterType)
        {
            case CharacterType.Erishikgal: return erishikgal != null ? erishikgal.meleeDamage : 0f;
            case CharacterType.Fredrick: return fredrick != null ? fredrick.meleeDamage : 0f;
            case CharacterType.Ezikiel: return ezikiel != null ? ezikiel.meleeDamage : 0f;
            case CharacterType.Miranda: return miranda != null ? miranda.meleeDamage : 0f;
            default: return 0f;
        }
    }

    float GetRangedDamage()
    {
        switch (activeCharacterType)
        {
            case CharacterType.Erishikgal: return erishikgal != null ? erishikgal.rangedDamage : 0f;
            case CharacterType.Fredrick: return fredrick != null ? fredrick.rangedDamage : 0f;
            case CharacterType.Ezikiel: return ezikiel != null ? ezikiel.rangedDamage : 0f;
            case CharacterType.Miranda: return miranda != null ? miranda.rangedDamage : 0f;
            default: return 0f;
        }
    }
}
