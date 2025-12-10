using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ErishikgalStats : MonoBehaviour
{
    [Header("Core Stats")]
    public float maxHealth = 125f;
    public float currentHealth = 125f;
    public float moveSpeed = 10f; // Matches PlayerController default
    public float sprintSpeed = 20f; // Added sprint speed

    [Header("Damage")]
    public float meleeDamage = 25f;
    public float rangedDamage = 15f;

    [Header("Gravity")]
    public float gravityScale = 1f; // Multiplier for gravity effect on Erishikgal

    [Header("Jump")]
    public float jumpForce = 7f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.mass = 1f * gravityScale; // Adjust mass based on gravityScale
        }
    }

    // Example method to apply gravity effect (if custom gravity is needed)
    void FixedUpdate()
    {
        if (rb != null)
        {
            rb.AddForce(Physics.gravity * gravityScale - Physics.gravity, ForceMode.Acceleration);
        }
    }
}
