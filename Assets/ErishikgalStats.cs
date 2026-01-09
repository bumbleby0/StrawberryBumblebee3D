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

    [Header("Melee Settings")]
    public float meleeRange = 2.5f; // Erishikgal uses spear-like gauntlet, decent reach
    public float meleeDelay = 0.4f; // skilled - not much delay

    [Header("Ranged Ammo")]
    public int maxAmmo = 6;
    public int currentAmmo = 6;
    public float rangedFireDelay = 0.08f; // very quick fire
    public float reloadTime = 4f;

    // runtime
    private bool isReloading = false;
    private float reloadTimer = 0f;
    private float lastRangedFireTime = -999f;

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

        // init ammo
        currentAmmo = Mathf.Clamp(currentAmmo, 0, maxAmmo);
    }
    void FixedUpdate()
    {
        if (rb != null)
        {
            rb.AddForce(Physics.gravity * gravityScale - Physics.gravity, ForceMode.Acceleration);
        }

        // reload ticking moved to PlayerController to centralize updates
    }

    public void UpdateRangedTimers(float dt)
    {
        if (isReloading)
        {
            reloadTimer -= dt;
            if (reloadTimer <= 0f)
            {
                isReloading = false;
                currentAmmo = maxAmmo;
            }
        }
    }
    
    public bool CanFireRanged()
    {
        if (isReloading) return false;
        if (currentAmmo <= 0) return false;
        if (Time.time < lastRangedFireTime + rangedFireDelay) return false;
        return true;
    }
    
    public bool TryFireRanged()
    {
        if (!CanFireRanged()) return false;
        currentAmmo = Mathf.Max(0, currentAmmo - 1);
        lastRangedFireTime = Time.time;
        if (currentAmmo <= 0)
            StartReload();
        return true;
    }
    
    public void StartReload()
    {
        if (isReloading) return;
        isReloading = true;
        reloadTimer = reloadTime;
    }
    
    public float GetReloadProgress()
    {
        if (!isReloading) return 1f;
        return Mathf.Clamp01(1f - (reloadTimer / reloadTime));
    }
    
    public int GetCurrentAmmo() => currentAmmo;
}
