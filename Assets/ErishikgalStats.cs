using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ErishikgalStats : MonoBehaviour
{
    [Header("Core Stats")]
    public float maxHealth = 125f;
    public float currentHealth = 125f;
    public float moveSpeed = 10f; 
    public float sprintSpeed = 20f; // Added sprint speed

    [Header("Combat Stats")]
    public float meleeDamage = 25f;
    public float rangedDamage = 15f;
    public float Defence = 15;
    public float Block = 35;
    public float BlockWalkSpeed = 5;
    public float InfernoCharge = 0f;
    public float MaxInfernoCharge = 100f;
    public float InfernoRechargeRatePerSecond = 1f;

    [Header("Gravity")]
    public float gravityScale = 0.9f; // Multiplier for gravity effect
    [Header("Jump")]
    public float jumpForce = 8f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.mass = 1f * gravityScale; // Adjust mass based on gravityScale
        }

        // If InfernoCharge wasn't set in Inspector, initialize it to full charge
        if (InfernoCharge <= 0f)
        {
            InfernoCharge = MaxInfernoCharge;
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
