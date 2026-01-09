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

    [Header("Melee Settings")]
    public float meleeRange = 2.8f; // longer than Erishikgal but Miranda is less skilled
    public float meleeDelay = 0.6f; // slower than others

    [Header("Ranged Ammo")]
    public int maxAmmo = 1;
    public int currentAmmo = 1;
    public float rangedFireDelay = 0.1f; // small initial delay
    public float reloadTime = 4f;

    // runtime
    private bool isReloading = false;
    private float reloadTimer = 0f;
    private float lastRangedFireTime = -999f;

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

    // Add charge to guided rocket bar; returns true if it reached full this update
    public bool AddGuidedCharge(float amount)
    {
        if (GuidedRocketCharge >= GuidedRocketChargeMax) return false;
        GuidedRocketCharge = Mathf.Min(GuidedRocketChargeMax, GuidedRocketCharge + amount);
        if (GuidedRocketCharge >= GuidedRocketChargeMax)
        {
            GuidedRocketReady = true;
            return true;
        }
        return false;
    }

    // Consume guided charge and mark not ready
    public bool ConsumeGuidedCharge()
    {
        if (!GuidedRocketReady && GuidedRocketCharge < GuidedRocketChargeMax) return false;
        GuidedRocketCharge = 0f;
        GuidedRocketReady = false;
        return true;
    }

    // Add charge to free rocket bar; when full, grant one free rocket (up to TempRocketMax)
    public bool AddFreeCharge(float amount)
    {
        if (TempRocketCount >= TempRocketMax) return false; // already has a free rocket
        FreeRocketCharge = Mathf.Min(MaxFreeRocketCharge, FreeRocketCharge + amount);
        if (FreeRocketCharge >= MaxFreeRocketCharge)
        {
            // grant free rocket
            TempRocketCount = Mathf.Min(TempRocketMax, TempRocketCount + 1);
            FreeRocketReady = TempRocketCount > 0;
            FreeRocketCharge = 0f;
            return true;
        }
        return false;
    }

    // Consume one free rocket if available
    public bool ConsumeFreeRocket()
    {
        if (TempRocketCount <= 0f) return false;
        TempRocketCount -= 1f;
        FreeRocketReady = TempRocketCount > 0f;
        return true;
    }
}
