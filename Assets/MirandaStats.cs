using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MirandaStats : MonoBehaviour
{
    [Header("Core Stats")]
    public float maxHealth = 50f;
    public float currentHealth = 50f;
    public float moveSpeed = 10f; // Matches PlayerController default
    public float sprintSpeed = 20f; // Added sprint speed

    [Header("Damage")]
    public float meleeDamage = 7f;
    public float rangedDamage = 65f;

    [Header("Gravity")]
    public float gravityScale = 1f; // Multiplier for gravity effect on Erishikgal

    [Header("Jump")]
    public float jumpForce = 6f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.mass = 0.95f * gravityScale; // Adjust mass based on gravityScale
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
