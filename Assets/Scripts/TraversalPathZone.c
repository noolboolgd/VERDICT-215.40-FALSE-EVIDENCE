using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TraversalPathZone : MonoBehaviour
{
    [Header("Path Anchors")]
    public Transform startAnchor;
    public Transform endAnchor;

    [Header("Optional Easing")]
    [Tooltip("Shapes the motion along the path. Leave linear for a straight staircase feel.")]
    public AnimationCurve easing = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Tooltip("If true, the player automatically detaches once t reaches 0 or 1 and re-enters free-run.")]
    public bool autoReleaseAtEnds = true;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }


    public Vector3 GetPointAtT(float t)
    {
        if (startAnchor == null || endAnchor == null)
        {
            return transform.position;
        };

        float eased = easing.Evaluate(Mathf.Clamp01(t));
        return Vector3.Lerp(startAnchor.position, endAnchor.position, eased);
    }

    /// <summary>
    /// Finds the closest normalized progress value on the path to a given world position.
    /// Used to smoothly hand off from free-run velocity into locked-path progress
    /// without the character snapping to the start of the path.
    /// </summary>
    public float GetClosestT(Vector3 worldPosition)
    {
        if (startAnchor == null || endAnchor == null)
        {
            return 0f;
        }

        Vector3 pathVector = endAnchor.position - startAnchor.position;
        float sqrLen = pathVector.sqrMagnitude;
        if (sqrLen < 0.0001f)
        {
            return 0f;
        }

        Vector3 toPoint = worldPosition - startAnchor.position;
        float t = Vector3.Dot(toPoint, pathVector) / sqrLen;
        return Mathf.Clamp01(t);
    }

    private void OnTriggerEnter(Collider other)
    {
        DepthTraversalController controller = other.GetComponent<DepthTraversalController>();
        if (controller != null)
        {
            controller.EnterPath(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        DepthTraversalController controller = other.GetComponent<DepthTraversalController>();
        if (controller != null)
        {
            controller.ExitPath(this);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (startAnchor == null || endAnchor == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(startAnchor.position, endAnchor.position);
        Gizmos.DrawSphere(startAnchor.position, 0.08f);
        Gizmos.DrawSphere(endAnchor.position, 0.08f);
    }
#endif
}
