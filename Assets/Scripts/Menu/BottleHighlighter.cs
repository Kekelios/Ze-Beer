using TMPro;
using UnityEngine;

public class BottleHighlighter : MonoBehaviour
{
    [Header("Highlight")]
    [SerializeField] private Light spotLight;               // optional (can be null)
    [SerializeField] private float intensityOff = 0f;
    [SerializeField] private float intensityOn = 3f;

    [Header("Bubble Label (optional)")]
    [SerializeField] private GameObject bubbleRoot;         // parent object for label UI
    [SerializeField] private TMP_Text bubbleText;           // label text

    public int BottleIndex { get; private set; } = -1;

    private string _label = string.Empty;
    private bool _isHovered;

    public void Initialize(int index, string label)
    {
        BottleIndex = index;
        _label = label ?? string.Empty;

        if (bubbleText != null)
            bubbleText.SetText(_label);

        SetHovered(false);
    }

    public void SetHovered(bool hovered)
    {
        if (_isHovered == hovered) return;
        _isHovered = hovered;

        if (spotLight != null)
            spotLight.intensity = hovered ? intensityOn : intensityOff;

        if (bubbleRoot != null)
            bubbleRoot.SetActive(hovered);
    }
}