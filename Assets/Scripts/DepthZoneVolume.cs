using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DepthZoneVolume : MonoBehaviour
{
    [Header("Depth Range (world Z)")]
    [Tooltip("Nearest allowed depth (closest to camera / foreground).")]
    public float minDepth = -2f;

    [Tooltip("Furthest allowed depth (background).")]
    public float maxDepth = 2f;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    /// <summary>
    /// puts a proposed world Z position to this zone's allowed depth range.
    /// </summary>
    public float ClampDepth(float worldZ)
    {
        return Mathf.Clamp(worldZ, minDepth, maxDepth);
    }

    private void OnTriggerEnter(Collider other)
    {
        DepthTraversalController controller = other.GetComponent<DepthTraversalController>();
        if (controller != null)
        {
            controller.EnterDepthZone(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        DepthTraversalController controller = other.GetComponent<DepthTraversalController>();
        if (controller != null)
        {
            controller.ExitDepthZone(this);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.25f);
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, col.bounds.size);
        }
    }
#endif
}
