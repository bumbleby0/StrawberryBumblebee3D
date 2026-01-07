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
    public float Block = 80; // flat damage reduced when blocking
    public float BlockWalkSpeed = 5;

    [Header("Absolute Defence")]
    [Tooltip("Charge gained per successful hit")]
    public int chargePerHit = 10;
    [Tooltip("Charge required to gain one Absolute Defence point")]
    public int chargeToPoint = 70;
    [Tooltip("Duration (seconds) that an activated Absolute Defence blocks the next incoming attack")]
    public float absoluteDefenceDuration = 3f;

    // runtime values
    public int absoluteDefencePoints = 0; // number of stored full points
    public float currentAbsoluteCharge = 0f; // partial charge towards next point
    private bool absoluteDefenceActive = false;
    private float absoluteDefenceTimer = 0f;

    [Header("Gravity")]
    public float gravityScale = 1.1f; // Multiplier for gravity effect

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

        // Start with 3 absolute defence points by default
        absoluteDefencePoints = 3;
    }
    void FixedUpdate()
    {
        if (rb != null)
        {
            rb.AddForce(Physics.gravity * gravityScale - Physics.gravity, ForceMode.Acceleration);
        }
    }

    void Update()
    {
        // countdown for active absolute defence
        if (absoluteDefenceActive)
        {
            absoluteDefenceTimer -= Time.deltaTime;
            if (absoluteDefenceTimer <= 0f)
            {
                absoluteDefenceActive = false;
                absoluteDefenceTimer = 0f;
            }
        }
    }

    // Call this when Fredrick lands a successful hit on an enemy
    public void AddChargeFromHit()
    {
        currentAbsoluteCharge += chargePerHit;
        while (currentAbsoluteCharge >= chargeToPoint)
        {
            currentAbsoluteCharge -= chargeToPoint;
            absoluteDefencePoints++;
            Debug.Log($"Fredrick gained an Absolute Defence point. Total points: {absoluteDefencePoints}");
        }
    }

    // Try to activate absolute defence (consumes one stored point and starts the 3s window)
    public bool TryActivateAbsoluteDefence()
    {
        if (absoluteDefencePoints > 0 && !absoluteDefenceActive)
        {
            absoluteDefencePoints--;
            absoluteDefenceActive = true;
            absoluteDefenceTimer = absoluteDefenceDuration;
            Debug.Log($"Fredrick activated Absolute Defence. Points remaining: {absoluteDefencePoints}");
            return true;
        }
        return false;
    }

    // If an incoming attack should be blocked, this consumes the active absolute defence and returns true.
    public bool ConsumeAbsoluteDefenceIfActive()
    {
        if (absoluteDefenceActive)
        {
            absoluteDefenceActive = false;
            absoluteDefenceTimer = 0f;
            Debug.Log("Fredrick used an Absolute Defence to block an attack");
            return true;
        }
        return false;
    }

    // New: resolve absolute defence against an incoming hit of given damage.
    // Returns true if the absolute defence consumed and fully blocked the hit.
    // If the incoming damage is <= Fredrick's base Defence, refunds the consumed point instead and returns false (no block used).
    public bool TryResolveAbsoluteDefenceOnIncomingDamage(float incomingDamage)
    {
        if (!absoluteDefenceActive)
            return false;

        // Use a small epsilon to avoid float precision issues when comparing equality
        const float EPS = 0.0001f;

        // If incoming damage is less than or equal to Fredrick's base Defence, refund the point;
        if (incomingDamage <= Defence + EPS)
        {
            // refund one point
            absoluteDefenceActive = false;
            absoluteDefenceTimer = 0f;
            absoluteDefencePoints++;
            Debug.Log($"Fredrick's Defence ({Defence}) covered the hit (incoming {incomingDamage}). Refunded an Absolute Defence point. Points now: {absoluteDefencePoints}");
            return false;
        }

        // Otherwise consume and block the hit
        absoluteDefenceActive = false;
        absoluteDefenceTimer = 0f;
        Debug.Log($"Fredrick used an Absolute Defence to block a heavy attack (incoming {incomingDamage})");
        return true;
    }

    // Support direct damage messages sent to Fredrick's component.
    public void TakeDamage(float damage)
    {
        // Forward damage handling to central PlayerController if available so Absolute Defence and blocking are applied consistently
        PlayerController pc = GetComponentInParent<PlayerController>();
        if (pc == null)
            pc = FindObjectOfType<PlayerController>();
        if (pc != null)
        {
            pc.TakeDamage(damage);
            return;
        }

        // Fallback: If no PlayerController found, apply damage directly
        if (ConsumeAbsoluteDefenceIfActive())
        {
            // Damage fully blocked
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        Debug.Log($"Fredrick took {damage} damage, health now {currentHealth}");
    }
}

