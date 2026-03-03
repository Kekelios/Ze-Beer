using System;
using UnityEngine;

[Serializable]
public class MenuBottleEntry
{
    [Header("Identity")]
    public string displayName;          // used by MenuFlowController error
    public string difficultyLabel;      // "Easy", "Medium", "Hard"
    public string gameSceneName;        // used by MenuFlowController error

    [Header("Scene References")]
    public Transform bottleTransform;   // optional: for camera focus
    public BottleHighlighter highlighter;

    /// <summary>Returns cached-style label text.</summary>
    public string GetLabel()
    {
        // No allocation issues here unless you call it every frame.
        // You call it on init / selection change: OK.
        if (string.IsNullOrEmpty(difficultyLabel)) return displayName ?? string.Empty;
        return $"{displayName} ({difficultyLabel})";
    }
}