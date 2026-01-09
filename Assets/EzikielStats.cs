using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EzikielStats : MonoBehaviour
{
    [Header("Core Stats")]
    public float maxHealth = 125f;
    public float currentHealth = 125f;
    public float moveSpeed = 8f;
    public float sprintSpeed = 15f; // Added sprint speed

    [Header("Combat Stats")]
    public float meleeDamage = 40f;
    public float rangedDamage = 35f;
    public float Defence = 20;
    public float Block = 25;
    public float BlockWalkSpeed = 6;
    public float RangedRageCount = 0;
    public float MeleeRageCount = 0;
    public float MeleeRageOnHit = 1;
    public float RangedRageOnHit = 1;
    public float AddedRangedDamage = 2;
    public float AddedMeleeDamage = 2;
    public float RageRangedDamage = 0;
    public float MeleeRageDamage = 0;

    [Header("Melee Settings")]
    public float meleeRange = 1.8f; // sword with finesse, shorter than Erishikgal
    public float meleeDelay = 0.45f; // skilled but slightly faster than average

    [Header("Ranged Ammo")]
    public int maxAmmo = 2;
    public int currentAmmo = 2;
    public float rangedFireDelay = 2f; // 2 seconds between each shot
    public float reloadTime = 4f;

    // runtime
    private bool isReloading = false;
    private float reloadTimer = 0f;
    private float lastRangedFireTime = -999f;

    [Header("Gravity")]
    public float gravityScale = 1f; // Multiplier for gravity effect

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

        // init ammo
        currentAmmo = Mathf.Clamp(currentAmmo, 0, maxAmmo);
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

    // Call when Ezi lands a successful melee attack
    // Builds stacks that increase ranged damage. Building ranged stacks clears melee stacks.
    public void AddRangedRageOnMelee()
    {
        // increment ranged-rage stacks
        RangedRageCount += RangedRageOnHit;
        // ensure melee stacks are cleared (can only build one kind at a time)
        MeleeRageCount = 0f;
        MeleeRageDamage = 0f;

        // update cached ranged bonus
        RageRangedDamage = RangedRageCount * AddedRangedDamage;
    }

    // Call when Ezi lands a successful ranged attack
    // Builds stacks that increase melee damage. Building melee stacks clears ranged stacks.
    public void AddMeleeRageOnRanged()
    {
        MeleeRageCount += MeleeRageOnHit;
        RangedRageCount = 0f;
        RageRangedDamage = 0f;

        MeleeRageDamage = MeleeRageCount * AddedMeleeDamage;
    }

    // Consume any built melee-rage and return the damage bonus. Resets melee stacks.
    public float ConsumeMeleeRageAndGetBonus()
    {
        if (MeleeRageCount <= 0f) return 0f;
        float bonus = MeleeRageCount * AddedMeleeDamage;
        MeleeRageCount = 0f;
        MeleeRageDamage = 0f;
        return bonus;
    }

    // Consume any built ranged-rage and return the damage bonus. Resets ranged stacks.
    public float ConsumeRangedRageAndGetBonus()
    {
        if (RangedRageCount <= 0f) return 0f;
        float bonus = RangedRageCount * AddedRangedDamage;
        RangedRageCount = 0f;
        RageRangedDamage = 0f;
        return bonus;
    }
}
