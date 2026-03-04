using System.Collections;
using UnityEngine;

public class MenuCameraRig : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;

    [Header("Plan Anchors")]
    [SerializeField] private Transform plan1Anchor;
    [SerializeField] private Transform plan2Anchor;

    [Header("Easing")]
    [SerializeField] private AnimationCurve easingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float transitionDuration = 1.5f;

    private void Awake()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main != null ? Camera.main.transform : null;
    }

    /// <summary>Déplace la caméra vers l'ancre du plan cible. Yield jusqu'à la fin.</summary>
    public IEnumerator MoveToPlan(MenuPlan plan)
    {
        Transform target = plan == MenuPlan.Plan1 ? plan1Anchor : plan2Anchor;

        if (target == null)
        {
            Debug.LogError($"[MenuCameraRig] Ancre manquante pour {plan}.");
            yield break;
        }

        yield return MoveToAnchor(target);
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
