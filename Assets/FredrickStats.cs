using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FredrickStats : MonoBehaviour
{
    [Header("Core Stats")]
    public float maxHealth = 175f;
    public float currentHealth = 175f;
    public float moveSpeed = 5f; // Matches PlayerController default
    public float sprintSpeed = 10f; // Added sprint speed

    [Header("Damage")]
    public float meleeDamage = 50f;
    public float rangedDamage = 0f;

    [Header("Gravity")]
    public float gravityScale = 1.2f; // Multiplier for gravity effect on Erishikgal

    [Header("Jump")]
    public float jumpForce = 3f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.mass = 1f * gravityScale; // Adjust mass based on gravityScale
        }
    }
    void FixedUpdate()
    {
        if (rb != null)
        {
            rb.AddForce(Physics.gravity * gravityScale - Physics.gravity, ForceMode.Acceleration);
        }
    }
}

