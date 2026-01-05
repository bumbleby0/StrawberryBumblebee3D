using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [Header("Health Bar")]
    public Image healthBarFill; // Assign your health bar Image in the Inspector

    [Header("Inferno Bar")]
    public GameObject infernoBarObject; // parent GameObject for the inferno bar (to show/hide)
    public Image infernoBarFill; // Assign Inferno bar Image in the Inspector

    // Call this method to update the health bar
    public void SetHealth(float currentHealth, float maxHealth)
    {
        if (healthBarFill != null && maxHealth > 0f)
        {
            healthBarFill.fillAmount = Mathf.Clamp01(currentHealth / maxHealth);
        }
    }

    // Call this to update the inferno bar fill
    public void SetInferno(float currentInferno, float maxInferno)
    {
        if (infernoBarFill != null && maxInferno > 0f)
        {
            infernoBarFill.fillAmount = Mathf.Clamp01(currentInferno / maxInferno);
        }
    }

    // Show or hide the inferno bar UI
    public void ShowInferno(bool show)
    {
        if (infernoBarObject != null)
            infernoBarObject.SetActive(show);
    }
}
