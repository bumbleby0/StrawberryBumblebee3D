using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [Header("Health Bar")]
    public Image healthBarFill; // Assign your health bar Image in the Inspector

    // Call this method to update the health bar
    public void SetHealth(float currentHealth, float maxHealth)
    {
        if (healthBarFill != null && maxHealth > 0f)
        {
            healthBarFill.fillAmount = Mathf.Clamp01(currentHealth / maxHealth);
        }
    }
}
