using System.Collections;
using UnityEngine;

public class MenuCameraRig : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;

    [Header("Plan Anchors")]
    [SerializeField] private Transform plan1Anchor;
    [SerializeField] private Transform plan2Anchor;
    [SerializeField] private Transform[] plan3Anchors; // one per bottle, same order as MenuFlowController.bottles

    [Header("Easing")]
    [SerializeField] private AnimationCurve easingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float transitionDuration = 1.5f;

    private void Awake()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main != null ? Camera.main.transform : null;
    }

    /// <summary>Moves the camera to the anchor for the given plan. Yields until complete.</summary>
    public IEnumerator MoveToPlan(MenuPlan plan, int bottleIndex = 0)
    {
        Transform target = GetAnchor(plan, bottleIndex);
        if (target == null)
        {
            Debug.LogError($"[MenuCameraRig] Missing anchor for plan {plan}, bottleIndex {bottleIndex}.");
            yield break;
        }
        yield return MoveToAnchor(target);
    }

    private Transform GetAnchor(MenuPlan plan, int bottleIndex)
    {
        return plan switch
        {
            MenuPlan.Plan1 => plan1Anchor,
            MenuPlan.Plan2 => plan2Anchor,
            MenuPlan.Plan3 when plan3Anchors != null && bottleIndex < plan3Anchors.Length
                => plan3Anchors[bottleIndex],
            _ => null
        };
    }

    private IEnumerator MoveToAnchor(Transform target)
    {
        if (cameraTransform == null) yield break;

        Vector3 startPos = cameraTransform.position;
        Quaternion startRot = cameraTransform.rotation;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = easingCurve.Evaluate(Mathf.Clamp01(elapsed / transitionDuration));
            cameraTransform.position = Vector3.Lerp(startPos, target.position, t);
            cameraTransform.rotation = Quaternion.Slerp(startRot, target.rotation, t);
            yield return null;
        }

        cameraTransform.position = target.position;
        cameraTransform.rotation = target.rotation;
    }
}
