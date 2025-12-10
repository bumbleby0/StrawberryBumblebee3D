using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Character Selection")]
    public ErishikgalStats activeCharacter; 

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
        rb = GetComponent<Rigidbody>();
        cam = GetComponentInChildren<Camera>();
        Cursor.lockState = CursorLockMode.Locked;
        if (cam != null)
            originalCameraLocalPos = cam.transform.localPosition;

        // Initialize health bar at start
        if (uiController != null && activeCharacter != null)
        {
            uiController.SetHealth(activeCharacter.currentHealth, activeCharacter.maxHealth);
        }
    }

    void Update()
    {
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
            else if (isGrounded && activeCharacter != null)
            {
                rb.AddForce(Vector3.up * activeCharacter.jumpForce, ForceMode.Impulse);
            }
        }

        // Self-damage on "P" key
        if (Input.GetKeyDown(KeyCode.P) && activeCharacter != null)
        {
            activeCharacter.currentHealth -= 25f;
            activeCharacter.currentHealth = Mathf.Max(0f, activeCharacter.currentHealth); // Prevent negative health

            if (uiController != null)
            {
                uiController.SetHealth(activeCharacter.currentHealth, activeCharacter.maxHealth);
            }
        }

        // Start slide
        if (Input.GetKeyDown(KeyCode.LeftControl) && isGrounded && !isSliding)
        {
            isSliding = true;

            // Calculate slide speed and duration based on current velocity
            float entrySpeed = rb.velocity.magnitude;
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
                if (jumpQueued && isGrounded && activeCharacter != null)
                {
                    rb.AddForce(Vector3.up * activeCharacter.jumpForce, ForceMode.Impulse);
                }
                jumpQueued = false;
            }
        }

        // Update health bar every frame (optional, if health can change elsewhere)
        if (uiController != null && activeCharacter != null)
        {
            uiController.SetHealth(activeCharacter.currentHealth, activeCharacter.maxHealth);
        }
    }

    void FixedUpdate()
    {
        if (activeCharacter == null)
            return; // Do nothing if no character is assigned

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // Only allow sprinting if grounded
        bool canSprint = isGrounded && Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = isSliding ? currentSlideSpeed : (canSprint ? activeCharacter.sprintSpeed : activeCharacter.moveSpeed);

        Vector3 move = (transform.right * moveX + transform.forward * moveZ).normalized * currentSpeed;
        Vector3 newPosition = rb.position + new Vector3(move.x, 0, move.z) * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);
    }
}
