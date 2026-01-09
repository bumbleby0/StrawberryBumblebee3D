using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Simple dialogue manager that can be called with any DialogueSequence asset to display a sequence of entries.
// Supports speaker name, text, portrait, and optional large images. Can be reused multiple times.
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject dialogueRoot; // parent to enable/disable
    public TMP_Text speakerNameText;
    public TMP_Text dialogueText;
    public Image portraitImage;
    public Image largeImage; // main large image placeholder

    [Header("Typing")]
    public float charsPerSecond = 40f;

    private DialogueSequence currentSequence;
    private int currentIndex = -1;
    private Coroutine typingCoroutine;
    private bool waitingForInput = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (dialogueRoot != null)
            dialogueRoot.SetActive(false);
    }

    // Start a sequence (can be called multiple times)
    public void StartSequence(DialogueSequence seq)
    {
        if (seq == null) return;
        currentSequence = seq;
        currentIndex = -1;
        if (dialogueRoot != null)
            dialogueRoot.SetActive(true);
        NextEntry();
    }

    // Advance to next entry
    public void NextEntry()
    {
        if (currentSequence == null) return;
        currentIndex++;
        if (currentIndex >= currentSequence.entries.Count)
        {
            EndSequence();
            return;
        }
        ShowEntry(currentSequence.entries[currentIndex]);
    }

    public void EndSequence()
    {
        StopTyping();
        currentSequence = null;
        currentIndex = -1;
        if (dialogueRoot != null)
            dialogueRoot.SetActive(false);
    }

    void ShowEntry(DialogueEntry entry)
    {
        StopTyping();
        if (speakerNameText != null)
            speakerNameText.text = entry.speaker ?? "";
        if (portraitImage != null)
            portraitImage.sprite = entry.portrait;
        // set main big image to first image if exists
        if (largeImage != null)
            largeImage.sprite = (entry.images != null && entry.images.Count > 0) ? entry.images[0] : null;

        if (entry.waitForInput)
        {
            typingCoroutine = StartCoroutine(TypeText(entry.text, null));
            waitingForInput = true;
        }
        else
        {
            typingCoroutine = StartCoroutine(TypeText(entry.text, entry.autoAdvanceSeconds));
            waitingForInput = false;
        }
    }

    IEnumerator TypeText(string text, float? autoAdvanceSeconds)
    {
        if (dialogueText == null) yield break;
        dialogueText.text = "";
        float delay = 1f / Mathf.Max(1f, charsPerSecond);
        for (int i = 0; i < text.Length; i++)
        {
            dialogueText.text += text[i];
            yield return new WaitForSeconds(delay);
            // allow skipping to full text on input
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            {
                dialogueText.text = text;
                break;
            }
        }

        // after typing completes
        if (autoAdvanceSeconds.HasValue)
        {
            yield return new WaitForSeconds(autoAdvanceSeconds.Value);
            NextEntry();
        }
        else
        {
            // wait for player input to advance
            while (!Input.GetMouseButtonDown(0) && !Input.GetKeyDown(KeyCode.Space))
                yield return null;
            NextEntry();
        }
    }

    void StopTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
    }
}
