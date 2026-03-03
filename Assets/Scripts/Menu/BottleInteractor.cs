using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class BottleInteractor : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private Camera menuCamera;
    [SerializeField] private LayerMask bottleLayerMask;

    private bool _isEnabled;
    private BottleHighlighter _currentHovered;
    private readonly List<RaycastResult> _uiRaycastResults = new List<RaycastResult>();

    private void Awake()
    {
        if (menuCamera == null)
            menuCamera = Camera.main;
    }

    private void Update()
    {
        if (!_isEnabled) return;

        if (IsPointerOverUI())
        {
            ClearHover();
            return;
        }

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = menuCamera.ScreenPointToRay(mousePos);

        // Visualise le ray dans la Scene view
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red);

        // Test sans LayerMask pour diagnostiquer
        if (Physics.Raycast(ray, out RaycastHit hitAll, Mathf.Infinity))
        {
            Debug.Log($"[BottleInteractor] Touche : {hitAll.collider.gameObject.name} | Layer : {LayerMask.LayerToName(hitAll.collider.gameObject.layer)} | Distance : {hitAll.distance:F2}");
        }
        else
        {
            Debug.Log("[BottleInteractor] Ne touche RIEN (sans mask)");
        }

        // Raycast réel avec LayerMask
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, bottleLayerMask))
        {
            BottleHighlighter highlighter = hit.collider.GetComponentInParent<BottleHighlighter>();
            if (highlighter != null)
            {
                if (highlighter != _currentHovered)
                {
                    ClearHover();
                    _currentHovered = highlighter;
                    _currentHovered.SetHovered(true);
                }

                if (Mouse.current.leftButton.wasPressedThisFrame)
                    MenuFlowController.Instance?.SelectBottleAndGoToPlan3(highlighter.BottleIndex);

                return;
            }
        }

        ClearHover();
    }

    /// <summary>Active ou désactive la détection hover/click sur les bouteilles.</summary>
    public void SetEnabled(bool enabled)
    {
        Debug.Log($"[BottleInteractor] SetEnabled({enabled})");
        _isEnabled = enabled;
        if (!enabled) ClearHover();
    }

    private void ClearHover()
    {
        if (_currentHovered == null) return;
        _currentHovered.SetHovered(false);
        _currentHovered = null;
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };

        _uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, _uiRaycastResults);
        return _uiRaycastResults.Count > 0;
    }
}
