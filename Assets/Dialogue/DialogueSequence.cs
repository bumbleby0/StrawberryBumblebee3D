using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Sequence", fileName = "NewDialogueSequence")]
public class DialogueSequence : ScriptableObject
{
    public List<DialogueEntry> entries = new List<DialogueEntry>();
}