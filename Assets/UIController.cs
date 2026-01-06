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

    [Header("Fredrick Absolute Defence Tokens")]
    public GameObject fredrickTokensParent; // parent object that will contain token instances
    public GameObject fredrickTokenPrefab; // prefab for a single token (assign in Inspector)

    private int currentFredrickTokenCount = -1;
    private readonly string[] fredrickTokenNames = { "FredrickToken_Left", "FredrickToken_Mid", "FredrickToken_Right" };

    void Start()
    {
        // Show the inferno bar only if Erishikgal is the selected character
        bool isErishikgal = !string.IsNullOrEmpty(CharacterSelector.SelectedCharacter) && CharacterSelector.SelectedCharacter == "Erishikgal";
        ShowInferno(isErishikgal);

        if (fredrickTokensParent != null)
            fredrickTokensParent.SetActive(false);
    }

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

    // Show or hide Fredrick token UI parent
    public void ShowFredrickTokens(bool show)
    {
        if (fredrickTokensParent != null)
            fredrickTokensParent.SetActive(show);
    }

    // Update the number of displayed Fredrick tokens to match 'count'
    // Tokens are ordered left->mid->right. They fill left-to-right; consumption removes the rightmost token first.
    public void SetFredrickTokens(int count)
    {
        if (fredrickTokensParent == null)
            return;

        count = Mathf.Clamp(count, 0, fredrickTokenNames.Length);
        if (count == currentFredrickTokenCount)
            return; // no change

        // Ensure three named children exist (left, mid, right)
        EnsureFredrickTokenSlotsExist();

        // Activate the leftmost 'count' tokens, deactivate others
        for (int i = 0; i < fredrickTokenNames.Length; i++)
        {
            Transform child = fredrickTokensParent.transform.Find(fredrickTokenNames[i]);
            if (child != null)
                child.gameObject.SetActive(i < count);
        }

        currentFredrickTokenCount = count;
    }

    // Ensure the parent contains three child slots named Left/Mid/Right. If prefab is assigned, instantiate copies; otherwise create empty GameObjects.
    private void EnsureFredrickTokenSlotsExist()
    {
        if (fredrickTokensParent == null)
            return;

        for (int i = 0; i < fredrickTokenNames.Length; i++)
        {
            string name = fredrickTokenNames[i];
            Transform existing = fredrickTokensParent.transform.Find(name);
            if (existing != null)
                continue;

            GameObject go = null;
            if (fredrickTokenPrefab != null)
            {
                go = Instantiate(fredrickTokenPrefab, fredrickTokensParent.transform);
                go.name = name;
            }
            else
            {
                go = new GameObject(name);
                go.transform.SetParent(fredrickTokensParent.transform, false);
                // add Image so it can be displayed if desired
                go.AddComponent<RectTransform>();
                var img = go.AddComponent<Image>();
                img.color = Color.white;
            }
        }

        // Ensure order left->mid->right in hierarchy
        for (int i = 0; i < fredrickTokenNames.Length; i++)
        {
            Transform t = fredrickTokensParent.transform.Find(fredrickTokenNames[i]);
            if (t != null)
                t.SetSiblingIndex(i);
        }
    }
}
