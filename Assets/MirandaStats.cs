using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MirandaStats : MonoBehaviour
{
    [Header("Core Stats")]
    public float maxHealth = 50f;
    public float currentHealth = 50f;
    public float moveSpeed = 10f;
    public float sprintSpeed = 20f; //sprint speed

    [Header("Combat Stats")]
    public float meleeDamage = 7f;
    public float rangedDamage = 65f;
    public float Defence = 5;
    public float Block = 10;
    public float BlockWalkSpeed = 10;
    public bool FreeRocketReady = false;
    public float MaxFreeRocketCharge = 100;
    public float FreeRocketCharge = 0;
    public float FreeRocketChargeSec = 2;
    public float GuidedRocketChargeSec = 4;
    public float GuidedRocketCharge = 0;
    public float GuidedRocketChargeMax = 100;
    public bool GuidedRocketReady = false;
    public float TempRocketMax = 1;
    public float TempRocketCount = 0;

    [Header("Gravity")]
    public float gravityScale = 1f; //Multiplier for gravity effect 
    [Header("Jump")]
    public float jumpForce = 8f;

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
