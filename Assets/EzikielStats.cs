using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EzikielStats : MonoBehaviour
{
    [Header("Core Stats")]
    public float maxHealth = 125f;
    public float currentHealth = 125f;
    public float moveSpeed = 8f; // Matches PlayerController default
    public float sprintSpeed = 15f; // Added sprint speed

    [Header("Damage")]
    public float meleeDamage = 40f;
    public float rangedDamage = 35f;

    [Header("Gravity")]
    public float gravityScale = 1f; // Multiplier for gravity effect on Ezikiel

    [Header("Jump")]
    public float jumpForce = 9f;

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
