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
    public UIController uiController;

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

    // Erishikgal Inferno mode (tap E to activate while charge > 1)
    private bool isInfernoActive = false;

    // Blocking
    public bool isBlocking = false;

    private int lastFredrickTokenCount = -99; // track last known count to update UI when changed

    // Respawn tracking
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    private float nextMeleeTime = 0f; // cooldown timer for melee attacks

    void Start()
    {
        // record initial spawn position/rotation for respawn
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

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

        // Initialize Fredrick token UI visibility and count if UIController is assigned
        if (uiController != null)
        {
            uiController.ShowFredrickTokens(activeCharacterType == CharacterType.Fredrick);
            if (activeCharacterType == CharacterType.Fredrick && fredrick != null)
            {
                uiController.SetFredrickTokens(fredrick.absoluteDefencePoints);
                lastFredrickTokenCount = fredrick.absoluteDefencePoints;
            }
            else
            {
                lastFredrickTokenCount = -99;
            }
        }
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

        // If sliding but become airborne, cancel slide to avoid MovePosition in air
        if (isSliding && !isGrounded)
        {
            isSliding = false;
            slideTimer = 0f;
            if (cam != null)
                cam.transform.localPosition = originalCameraLocalPos;
        }

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

        // Self-damage on "P" key (debug): route through TakeDamage so blocking/Absolute Defence apply
        if (Input.GetKeyDown(KeyCode.P))
        {
            TakeDamage(25f);
        }

        // Debug: deal 100 damage to the active character when pressing O
        if (Input.GetKeyDown(KeyCode.O))
        {
            TakeDamage(100f);
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

            // Miranda UI updates: guided and free rocket bars
            if (miranda != null)
            {
                uiController.ShowMirandaUI(activeCharacterType == CharacterType.Miranda);
                uiController.SetMirandaGuidedProgress(miranda.GuidedRocketCharge, miranda.GuidedRocketChargeMax);
                uiController.SetMirandaFreeProgress(miranda.FreeRocketCharge, miranda.MaxFreeRocketCharge);
            }
        }

        // Handle attack input: Left click to attack, R to toggle melee/ranged (Fredrick cannot toggle)
        if (Input.GetKeyDown(KeyCode.R))
        {
            ToggleAttackMode();
        }
        if (Input.GetMouseButtonDown(0))
        {
            if (currentAttackMode == AttackMode.Melee)
            {
                // Respect per-character melee delay
                if (Time.time >= nextMeleeTime)
                {
                    DoMeleeAttack();
                    nextMeleeTime = Time.time + GetMeleeDelayForActive();
                }
                else
                {
                    // optionally ignore or give feedback
                    Debug.Log("Melee on cooldown");
                }
            }
            else
            {
                // Ranged fire: only if active character has ammo and is allowed to fire
                bool fired = false;
                switch (activeCharacterType)
                {
                    case CharacterType.Erishikgal:
                        if (erishikgal != null && erishikgal.TryFireRanged()) fired = true;
                        break;
                    case CharacterType.Fredrick:
                        // Fredrick has no ranged attacks
                        break;
                    case CharacterType.Ezikiel:
                        if (ezikiel != null && ezikiel.TryFireRanged()) fired = true;
                        break;
                    case CharacterType.Miranda:
                        if (miranda != null && miranda.TryFireRanged()) fired = true;
                        break;
                }

                if (fired)
                {
                    DoRangedAttack();
                }
                else
                {
                    Debug.Log("Ranged cannot fire - no ammo or reloading");
                }
            }
        }

        // Erishikgal Inferno input: hold E to activate while Erishikgal and has charge > 1
        if (activeCharacterType == CharacterType.Erishikgal && erishikgal != null)
        {
            bool wantInferno = Input.GetKey(KeyCode.E) && erishikgal.InfernoCharge > 1f;
            if (wantInferno && !isInfernoActive)
            {
                isInfernoActive = true;
            }
            else if (!wantInferno && isInfernoActive)
            {
                isInfernoActive = false;
            }

            // If active, drain over time here (use Time.deltaTime for visible UI update)
            if (isInfernoActive)
            {
                float drainPerSecond = (erishikgal.MaxInfernoCharge > 0f) ? (erishikgal.MaxInfernoCharge / 8f) : (100f / 8f);
                float delta = drainPerSecond * Time.deltaTime;
                erishikgal.InfernoCharge = Mathf.Max(0f, erishikgal.InfernoCharge - delta);

                if (erishikgal.InfernoCharge <= 0f)
                {
                    isInfernoActive = false;
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
                    erishikgal.InfernoCharge = Mathf.Min(erishikgal.MaxInfernoCharge, erishikgal.InfernoCharge + recharge);
                    if (uiController != null)
                        uiController.SetInferno(erishikgal.InfernoCharge, erishikgal.MaxInfernoCharge);
                }
            }
        }

        // Blocking input and Absolute Defence activation
        // Universal block: holding right mouse applies a flat Block reduction based on the currently active character's Block stat.
        // Absolute Defence activation (Fredrick) can be triggered by pressing E even if Fredrick is not the active character,
        // but Erishikgal's inferno (hold E) takes precedence when Erishikgal is active.

        // Try to activate Fredrick Absolute Defence when E is pressed and Erishikgal is not active
        if (Input.GetKeyDown(KeyCode.E) && !(activeCharacterType == CharacterType.Erishikgal))
        {
            if (activeCharacterType == CharacterType.Miranda && miranda != null)
            {
                // If guided rocket is full, pressing E should enter target painting mode (consume charge)
                if (miranda.GuidedRocketCharge >= miranda.GuidedRocketChargeMax)
                {
                    bool consumed = miranda.ConsumeGuidedCharge();
                    if (consumed)
                    {
                        EnterMirandaTargetPaintMode();
                    }
                }
                else
                {
                    // If not Miranda or guided not ready, try Fredrick absolute defence as before
                    if (fredrick != null)
                    {
                        bool activated = fredrick.TryActivateAbsoluteDefence();
                        if (activated)
                        {
                            if (uiController != null)
                                uiController.SetFredrickTokens(fredrick.absoluteDefencePoints);
                        }
                    }
                }
            }
            else
            {
                if (fredrick != null)
                {
                    bool activated = fredrick.TryActivateAbsoluteDefence();
                    if (activated)
                    {
                        // logged inside TryActivateAbsoluteDefence
                        if (uiController != null)
                            uiController.SetFredrickTokens(fredrick.absoluteDefencePoints);
                    }
                }
            }
        }

        // Universal blocking hold
        bool holdingBlock = Input.GetMouseButton(1);
        if (holdingBlock && !isBlocking)
        {
            isBlocking = true;
            Debug.Log("Started blocking (hold right mouse)");
        }
        else if (!holdingBlock && isBlocking)
        {
            isBlocking = false;
            Debug.Log("Stopped blocking");
        }

        // Poll Fredrick token count and update UI when it changes (covers any consumption path)
        if (uiController != null && fredrick != null)
        {
            if (activeCharacterType == CharacterType.Fredrick)
            {
                if (fredrick.absoluteDefencePoints != lastFredrickTokenCount)
                {
                    uiController.SetFredrickTokens(fredrick.absoluteDefencePoints);
                    lastFredrickTokenCount = fredrick.absoluteDefencePoints;
                }
            }
            else
            {
                // hide tokens when not Fredrick
                if (lastFredrickTokenCount != -99)
                {
                    uiController.ShowFredrickTokens(false);
                    lastFredrickTokenCount = -99;
                }
            }
        }
    }

    void FixedUpdate()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");
        
        Vector3 moveInput = (transform.right * moveX + transform.forward * moveZ).normalized;

        bool canSprint = isGrounded && Input.GetKey(KeyCode.LeftShift) && !isBlocking;
        float currentSpeed = isSliding ? currentSlideSpeed : (canSprint ? GetSprintSpeed() : GetMoveSpeed());
        if (isInfernoActive && activeCharacterType == CharacterType.Erishikgal)
            currentSpeed *= 2f;

        Vector3 velocity = rb.velocity;

        if (isGrounded || isSliding)
        {
            if (moveInput.sqrMagnitude > 0.01f)
            {
                Vector3 move = moveInput * currentSpeed * Time.fixedDeltaTime;
                rb.MovePosition(rb.position + new Vector3(move.x, 0, move.z));
            }
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

        // Miranda passive charge accumulation while Miranda is active (or optionally always)
        if (miranda != null)
        {
            // Free rocket charge builds at FreeRocketChargeSec per second
            miranda.AddFreeCharge(miranda.FreeRocketChargeSec * Time.fixedDeltaTime);
            // Guided rocket charge builds at GuidedRocketChargeSec per second
            miranda.AddGuidedCharge(miranda.GuidedRocketChargeSec * Time.fixedDeltaTime);

            if (uiController != null && activeCharacterType == CharacterType.Miranda)
            {
                uiController.SetMirandaGuidedProgress(miranda.GuidedRocketCharge, miranda.GuidedRocketChargeMax);
                uiController.SetMirandaFreeProgress(miranda.FreeRocketCharge, miranda.MaxFreeRocketCharge);
            }
        }

        // Update ranged reload timers for characters that have them
        float dt = Time.fixedDeltaTime;
        if (erishikgal != null) erishikgal.UpdateRangedTimers(dt);
        if (ezikiel != null) ezikiel.UpdateRangedTimers(dt);
        if (miranda != null) miranda.UpdateRangedTimers(dt);
    }

    // --- Character Swapping Helpers ---

    void SetActiveCharacter(CharacterType type)
    {
        activeCharacterType = type;
        SetHealthUI();

        if (uiController != null)
        {
            uiController.ShowFredrickTokens(activeCharacterType == CharacterType.Fredrick);
            if (activeCharacterType == CharacterType.Fredrick && fredrick != null)
            {
                uiController.SetFredrickTokens(fredrick.absoluteDefencePoints);
                lastFredrickTokenCount = fredrick.absoluteDefencePoints;
            }
            else
            {
                lastFredrickTokenCount = -99;
            }

            // Show or hide Ezikiel rage UI
            uiController.ShowEzikielRage(activeCharacterType == CharacterType.Ezikiel);
            if (activeCharacterType == CharacterType.Ezikiel && ezikiel != null)
            {
                uiController.SetEzikielRage((int)ezikiel.MeleeRageCount, (int)ezikiel.RangedRageCount);
            }
        }
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
        float rangeForThis = GetMeleeRangeForActive();
        Vector3 center = transform.position + transform.forward * (rangeForThis * 0.5f) + Vector3.up * 1f;
        Collider[] hits = Physics.OverlapSphere(center, meleeRadius, attackMask);

        // If Ezikiel is active and there will be at least one hit, consume melee-rage (built by ranged attacks)
        if (activeCharacterType == CharacterType.Ezikiel && ezikiel != null && hits.Length > 0)
        {
            float bonus = ezikiel.ConsumeMeleeRageAndGetBonus();
            if (bonus != 0f)
            {
                damage += bonus;
                Debug.Log($"Ezikiel consumed Melee Rage for +{bonus} melee damage");
            }
            if (uiController != null)
                uiController.SetEzikielRage((int)ezikiel.MeleeRageCount, (int)ezikiel.RangedRageCount);
        }

        foreach (var col in hits)
        {
            if (col == null) continue;
            if (col.transform.IsChildOf(transform) || col.gameObject == gameObject) continue;
            col.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);

            // If Fredrick is the active character, gain charge per successful hit
            if (activeCharacterType == CharacterType.Fredrick && fredrick != null)
            {
                fredrick.AddChargeFromHit();
                if (uiController != null)
                    uiController.SetFredrickTokens(fredrick.absoluteDefencePoints);
            }

            // If Ezikiel is the active character, successful melee hit builds a ranged-rage stack
            if (activeCharacterType == CharacterType.Ezikiel && ezikiel != null)
            {
                ezikiel.AddRangedRageOnMelee();
                if (uiController != null)
                {
                    uiController.SetEzikielRage((int)ezikiel.MeleeRageCount, (int)ezikiel.RangedRageCount);
                }
            }
        }
        Debug.Log($"Melee attack dealt {damage} to {hits.Length} colliders");
        #if UNITY_EDITOR
        Debug.DrawLine(transform.position + Vector3.up * 1f, center, Color.red, 0.5f);
        #endif
    }

    // Return the melee range for the currently active character
    float GetMeleeRangeForActive()
    {
        switch (activeCharacterType)
        {
            case CharacterType.Erishikgal: return erishikgal != null ? erishikgal.meleeRange : meleeRange;
            case CharacterType.Fredrick: return fredrick != null ? fredrick.meleeRange : meleeRange;
            case CharacterType.Ezikiel: return ezikiel != null ? ezikiel.meleeRange : meleeRange;
            case CharacterType.Miranda: return miranda != null ? miranda.meleeRange : meleeRange;
            default: return meleeRange;
        }
    }

    // Return the melee attack delay (seconds) for the currently active character
    float GetMeleeDelayForActive()
    {
        switch (activeCharacterType)
        {
            case CharacterType.Erishikgal: return erishikgal != null ? erishikgal.meleeDelay : 0.5f;
            case CharacterType.Fredrick: return fredrick != null ? fredrick.meleeDelay : 0.5f;
            case CharacterType.Ezikiel: return ezikiel != null ? ezikiel.meleeDelay : 0.5f;
            case CharacterType.Miranda: return miranda != null ? miranda.meleeDelay : 0.5f;
            default: return 0.5f;
        }
    }

    void DoRangedAttack()
    {
        float damage = GetRangedDamage();
        Ray ray = (cam != null) ? new Ray(cam.transform.position, cam.transform.forward) : new Ray(transform.position + Vector3.up * 1f, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, rangedMaxDistance, attackMask))
        {
            // If Ezikiel is active, consume ranged-rage (built by melee attacks) when the shot hits
            if (activeCharacterType == CharacterType.Ezikiel && ezikiel != null)
            {
                float bonus = ezikiel.ConsumeRangedRageAndGetBonus();
                if (bonus != 0f)
                {
                    damage += bonus;
                    Debug.Log($"Ezikiel consumed Ranged Rage for +{bonus} ranged damage");
                }
                if (uiController != null)
                    uiController.SetEzikielRage((int)ezikiel.MeleeRageCount, (int)ezikiel.RangedRageCount);
            }

            hit.collider.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);

            // If Fredrick is the active character, gain charge per successful hit
            if (activeCharacterType == CharacterType.Fredrick && fredrick != null)
            {
                fredrick.AddChargeFromHit();
                if (uiController != null)
                    uiController.SetFredrickTokens(fredrick.absoluteDefencePoints);
            }

            // If Ezikiel is the active character, successful ranged hit builds a melee-rage stack
            if (activeCharacterType == CharacterType.Ezikiel && ezikiel != null)
            {
                ezikiel.AddMeleeRageOnRanged();
                if (uiController != null)
                {
                    uiController.SetEzikielRage((int)ezikiel.MeleeRageCount, (int)ezikiel.RangedRageCount);
                }
            }

            Debug.Log($"Ranged attack hit {hit.collider.name} for {damage}");
        }
        else
        {
            Debug.Log($"Ranged attack missed (dealt {damage} to nothing)");
        }
    }

    // Receive damage via SendMessage from other objects
    public void TakeDamage(float damage)
    {
        // If Fredrick has an active Absolute Defence and Fredrick is the active character, let him resolve it against this incoming damage.
        // This keeps damage retrieval for other characters unchanged.
        if (activeCharacterType == CharacterType.Fredrick && fredrick != null)
        {
            bool resolved = fredrick.TryResolveAbsoluteDefenceOnIncomingDamage(damage);
            if (uiController != null)
                uiController.SetFredrickTokens(fredrick.absoluteDefencePoints);
            if (resolved)
            {
                Debug.Log("Fredrick blocked incoming damage with Absolute Defence");
                return;
            }
        }

        // Apply passive defence of the character receiving damage first
        float defence = 0f;
        switch (activeCharacterType)
        {
            case CharacterType.Erishikgal: if (erishikgal != null) defence = erishikgal.Defence; break;
            case CharacterType.Fredrick: if (fredrick != null) defence = fredrick.Defence; break;
            case CharacterType.Ezikiel: if (ezikiel != null) defence = ezikiel.Defence; break;
            case CharacterType.Miranda: if (miranda != null) defence = miranda.Defence; break;
        }

        float beforeDef = damage;
        damage = Mathf.Max(0f, damage - defence);
        if (defence > 0f)
            Debug.Log($"Applied defence {defence}. Incoming: {beforeDef} -> {damage}");

        // If blocking (universal) subtract flat Block value from damage (additive with defence)
        if (isBlocking)
        {
            float blockValue = 0f;
            switch (activeCharacterType)
            {
                case CharacterType.Erishikgal: if (erishikgal != null) blockValue = erishikgal.Block; break;
                case CharacterType.Fredrick: if (fredrick != null) blockValue = fredrick.Block; break;
                case CharacterType.Ezikiel: if (ezikiel != null) blockValue = ezikiel.Block; break;
                case CharacterType.Miranda: if (miranda != null) blockValue = miranda.Block; break;
            }

            float beforeBlock = damage;
            damage = Mathf.Max(0f, damage - blockValue);
            if (blockValue > 0f)
                Debug.Log($"Applied block {blockValue}. Before block: {beforeBlock} -> {damage}");
        }

        // Apply damage to the active character
        float newHealth = Mathf.Max(0f, GetCurrentHealth() - damage);
        SetCurrentHealth(newHealth);
        SetHealthUI();

        // If health reached zero, handle death / respawn
        if (Mathf.Approximately(newHealth, 0f) || newHealth <= 0f)
        {
            HandlePlayerDeath();
        }
    }

    // Handle player death: teleport back to spawn and restore active character health
    void HandlePlayerDeath()
    {
        Debug.Log("Player died - respawning at spawn point");
        // teleport player to spawn position and rotation
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = spawnPosition;
        }
        transform.position = spawnPosition;
        transform.rotation = spawnRotation;

        // reset some transient states
        isSliding = false;
        isBlocking = false;
        isInfernoActive = false;

        // restore current active character's health to max
        SetCurrentHealth(GetMaxHealth());
        SetHealthUI();
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

    // Enter target painting mode for Miranda; basic implementation: raycast to select a single enemy under crosshair and spawn a guided rocket that follows it.
    void EnterMirandaTargetPaintMode()
    {
        Debug.Log("Entering Miranda target paint mode");
        // Simple implementation: immediate select whatever is under the center of the camera and fire a guided rocket prefab that will home in.
        if (cam == null)
            return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, rangedMaxDistance, attackMask))
        {
            GameObject target = hit.collider.gameObject;
            // Spawn guided rocket if prefab exists in Resources/Prefabs/MirandaGuidedRocket
            GameObject rocketPrefab = Resources.Load<GameObject>("Prefabs/MirandaGuidedRocket");
            if (rocketPrefab != null)
            {
                Vector3 spawnPos = cam.transform.position + cam.transform.forward * 1f;
                GameObject rocket = Instantiate(rocketPrefab, spawnPos, Quaternion.identity);
                var homing = rocket.GetComponent<MirandaGuidedRocket>();
                if (homing != null)
                {
                    homing.SetTarget(target.transform);
                }
            }
            else
            {
                Debug.LogWarning("Miranda guided rocket prefab not found at Resources/Prefabs/MirandaGuidedRocket");
            }
        }
        else
        {
            Debug.Log("No target under crosshair to paint for guided rocket");
        }
    }

    // Fire a free rocket if Miranda has one available. This is a simple stub that consumes the free rocket and spawns a rocket prefab.
    void FireMirandaFreeRocket()
    {
        if (miranda == null) return;
        if (!miranda.ConsumeFreeRocket())
        {
            Debug.Log("No free rockets available");
            return;
        }

        GameObject rocketPrefab = Resources.Load<GameObject>("Prefabs/MirandaFreeRocket");
        if (rocketPrefab != null)
        {
            Vector3 spawnPos = cam != null ? cam.transform.position + cam.transform.forward * 1f : transform.position + transform.forward * 1f + Vector3.up * 1f;
            Instantiate(rocketPrefab, spawnPos, Quaternion.LookRotation(cam != null ? cam.transform.forward : transform.forward));
            Debug.Log("Fired a free Miranda rocket");
        }
        else
        {
            Debug.LogWarning("Miranda free rocket prefab not found at Resources/Prefabs/MirandaFreeRocket");
        }
    }
}
