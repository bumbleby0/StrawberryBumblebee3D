using System.Collections;
using System.Collections.Generic;
using TMPro;
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

    [Header("MirandaGuidedRocketBar")]
    public GameObject MirandaGuidingProgressBG; // parent GameObject for Guided Rocket Bar
    public Image MirandaGuidingProgress; // Assign Guided Rocket Bar Image in Inspector

    [Header("MirandaFreeRocketBar")]
    public GameObject MirandaFreeRocketBG; // parent GameObject for Free Rocket Bar
    public Image MirandaFreeRocketBar; // Assign Free Rocket Bar Image in Inspector

    [Header("Ezikiel Rage Parent")]
    public GameObject EzikielRageParent; // parent GameObject for the Rage
    public Image EziMeleeRageimg; // Assign Melee Rage image in Inspector 
    public Image EziRangedRageimg; // Assign Ranged Rage image in Inspector
    public TextMeshProUGUI EziMeleeRageCount; // Assign Melee Rage count in Inspector
    public TextMeshProUGUI EziRangedRageCount; // Assign Ranged Rage count in Inspector

    [Header("Dialogue UI")]
    public GameObject dialogueRoot;
    public TMPro.TMP_Text dialogueSpeakerName;
    public TMPro.TMP_Text dialogueText;
    public Image dialoguePortrait;
    public Image dialogueLargeImage;

    private int currentFredrickTokenCount = -1;
    private readonly string[] fredrickTokenNames = { "FredrickToken_Left", "FredrickToken_Mid", "FredrickToken_Right" };

    // smoothing targets / state
    private float targetHealthFill = 1f;
    private float displayedHealthFill = 1f;
    private const float healthSmoothTime = 0.25f; // seconds to reach target for health

    private float targetInfernoFill = 0f;
    private float displayedInfernoFill = 0f;
    private const float otherSmoothTime = 0.5f; // seconds to reach target for other bars (drop/build)

    private float targetMirandaGuidedFill = 0f;
    private float displayedMirandaGuidedFill = 0f;

    private float targetMirandaFreeFill = 0f;
    private float displayedMirandaFreeFill = 0f;

    void Start()
    {
        // Show the inferno bar only if Erishikgal is the selected character
        bool isErishikgal = !string.IsNullOrEmpty(CharacterSelector.SelectedCharacter) && CharacterSelector.SelectedCharacter == "Erishikgal";
        ShowInferno(isErishikgal);
        // Show Miranda's UI only if Miranda is the selected character
        bool isMiranda = !string.IsNullOrEmpty(CharacterSelector.SelectedCharacter) && CharacterSelector.SelectedCharacter == "Miranda";
        ShowMirandaUI(isMiranda);
        // Show the Ezikiel UI only if Ezikiel is the selected character
        bool isEzikel = !string.IsNullOrEmpty(CharacterSelector.SelectedCharacter) && CharacterSelector.SelectedCharacter == "Ezikiel";
        ShowEzikielRage(isEzikel);


        if (fredrickTokensParent != null)
            fredrickTokensParent.SetActive(false);

        // initialize displayed fills from current images if available
        if (healthBarFill != null)
        {
            displayedHealthFill = healthBarFill.fillAmount;
            targetHealthFill = displayedHealthFill;
        }
        if (infernoBarFill != null)
        {
            displayedInfernoFill = infernoBarFill.fillAmount;
            targetInfernoFill = displayedInfernoFill;
        }
        if (MirandaGuidingProgress != null)
        {
            displayedMirandaGuidedFill = MirandaGuidingProgress.fillAmount;
            targetMirandaGuidedFill = displayedMirandaGuidedFill;
        }
        if (MirandaFreeRocketBar != null)
        {
            displayedMirandaFreeFill = MirandaFreeRocketBar.fillAmount;
            targetMirandaFreeFill = displayedMirandaFreeFill;
        }
    }

    void Update()
    {
        // Smoothly move displayed fills toward target fills over configured durations
        float dt = Time.unscaledDeltaTime; // use unscaled so UI isn't affected by time scale changes

        // Health
        if (healthBarFill != null)
        {
            if (!Mathf.Approximately(displayedHealthFill, targetHealthFill))
            {
                float step = dt / Mathf.Max(0.0001f, healthSmoothTime);
                displayedHealthFill = Mathf.MoveTowards(displayedHealthFill, targetHealthFill, step);
                healthBarFill.fillAmount = displayedHealthFill;
            }
        }

        // Inferno
        if (infernoBarFill != null)
        {
            if (!Mathf.Approximately(displayedInfernoFill, targetInfernoFill))
            {
                float step = dt / Mathf.Max(0.0001f, otherSmoothTime);
                displayedInfernoFill = Mathf.MoveTowards(displayedInfernoFill, targetInfernoFill, step);
                infernoBarFill.fillAmount = displayedInfernoFill;
            }
        }

        // Miranda guided
        if (MirandaGuidingProgress != null)
        {
            if (!Mathf.Approximately(displayedMirandaGuidedFill, targetMirandaGuidedFill))
            {
                float step = dt / Mathf.Max(0.0001f, otherSmoothTime);
                displayedMirandaGuidedFill = Mathf.MoveTowards(displayedMirandaGuidedFill, targetMirandaGuidedFill, step);
                MirandaGuidingProgress.fillAmount = displayedMirandaGuidedFill;
            }
        }

        // Miranda free
        if (MirandaFreeRocketBar != null)
        {
            if (!Mathf.Approximately(displayedMirandaFreeFill, targetMirandaFreeFill))
            {
                float step = dt / Mathf.Max(0.0001f, otherSmoothTime);
                displayedMirandaFreeFill = Mathf.MoveTowards(displayedMirandaFreeFill, targetMirandaFreeFill, step);
                MirandaFreeRocketBar.fillAmount = displayedMirandaFreeFill;
            }
        }
    }

    // Call this method to update the health bar (sets target; transition occurs in Update())
    public void SetHealth(float currentHealth, float maxHealth)
    {
        if (healthBarFill != null && maxHealth > 0f)
        {
            float target = Mathf.Clamp01(currentHealth / maxHealth);
            targetHealthFill = target;
            // if the UI is offscreen or we want instant set when large jumps (optional), we can set displayed immediately when difference tiny
        }
    }

    // Call this to update the inferno bar fill (sets target)
    public void SetInferno(float currentInferno, float maxInferno)
    {
        if (infernoBarFill != null && maxInferno > 0f)
        {
            targetInfernoFill = Mathf.Clamp01(currentInferno / maxInferno);
        }
    }

    // Show or hide the inferno bar UI
    public void ShowInferno(bool show)
    {
        if (infernoBarObject != null)
            infernoBarObject.SetActive(show);
    }

    // Show or hide Miranda UI elements 
    public void ShowMirandaUI(bool show)
    {
        if (MirandaGuidingProgressBG != null)
            MirandaGuidingProgressBG.SetActive(show);
        if (MirandaFreeRocketBG != null)
            MirandaFreeRocketBG.SetActive(show);
    }

    // Update Miranda guided rocket charge bar (sets target)
    public void SetMirandaGuidedProgress(float current, float max)
    {
        if (MirandaGuidingProgress != null && max > 0f)
        {
            targetMirandaGuidedFill = Mathf.Clamp01(current / max);
        }
    }

    // Update Miranda free rocket charge bar (sets target)
    public void SetMirandaFreeProgress(float current, float max)
    {
        if (MirandaFreeRocketBar != null && max > 0f)
        {
            targetMirandaFreeFill = Mathf.Clamp01(current / max);
        }
    }

    // Sshow or hide Ezikiel UI elements
    public void ShowEzikielRage(bool show)
    {
        if (EzikielRageParent != null)
            EzikielRageParent.SetActive(show);
    }

    // Update Ezikiel's rage counters and visibility of their icons
    public void SetEzikielRage(int meleeCount, int rangedCount)
    {
        if (EziMeleeRageCount != null)
            EziMeleeRageCount.text = meleeCount.ToString();
        if (EziRangedRageCount != null)
            EziRangedRageCount.text = rangedCount.ToString();

        if (EziMeleeRageimg != null)
            EziMeleeRageimg.gameObject.SetActive(meleeCount > 0);
        if (EziRangedRageimg != null)
            EziRangedRageimg.gameObject.SetActive(rangedCount > 0);
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

    // Hook to show/hide dialogue root quickly
    public void ShowDialogueUI(bool show)
    {
        if (dialogueRoot != null)
            dialogueRoot.SetActive(show);
    }

    // Set dialogue fields (optional convenience)
    public void SetDialogueSpeaker(string name)
    {
        if (dialogueSpeakerName != null)
            dialogueSpeakerName.text = name;
    }
    public void SetDialogueText(string t)
    {
        if (dialogueText != null)
            dialogueText.text = t;
    }
    public void SetDialoguePortrait(Sprite s)
    {
        if (dialoguePortrait != null)
            dialoguePortrait.sprite = s;
    }
    public void SetDialogueLargeImage(Sprite s)
    {
        if (dialogueLargeImage != null)
            dialogueLargeImage.sprite = s;
    }
}
