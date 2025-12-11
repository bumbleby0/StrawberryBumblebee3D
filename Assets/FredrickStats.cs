using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FredrickStats : MonoBehaviour
{
    [Header("Core Stats")]
    public float maxHealth = 175f;
    public float currentHealth = 175f;
    public float moveSpeed = 5f;
    public float sprintSpeed = 10f; // Added sprint speed

    [Header("Combat Stats")]
    public float meleeDamage = 50f;
    public float rangedDamage = 0f;
    public float Defence = 40;
    public float Block = 80;
    public float BlockWalkSpeed = 5;

    [Header("Gravity")]
    public float gravityScale = 1.2f; // Multiplier for gravity effect

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

