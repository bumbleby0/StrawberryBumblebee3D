using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueEntry
{
    public string speaker;
    [TextArea(2,6)]
    public string text;
    public Sprite portrait; // small speaker portrait

    // optional large images (backgrounds, art, etc.)
    public List<Sprite> images;

    // if true, wait for player input to advance, otherwise advance automatically after duration
    public bool waitForInput = true;
    public float autoAdvanceSeconds = 2.0f;

    // constructor convenience
    public DialogueEntry() { images = new List<Sprite>(); }
}