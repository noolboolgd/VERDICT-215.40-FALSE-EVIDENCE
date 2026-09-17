using UnityEngine;

[RequireComponent(typeof(Collider))]
public class StairZone : MonoBehaviour
{
    public enum StairType { Diagonal, Depth }

    public StairType type = StairType.Diagonal;

    [Header("Diagonal Stairs Settings")]
    public Vector2 slopeDirection = new Vector2(1, 1);

    [Header("Depth Stairs Settings")]
    [Tooltip("World Z of the bottom of the stairs (closest to camera).")]
    public float nearZ = 0f;
    [Tooltip("World Z of the top of the stairs (furthest from camera).")]
    public float farZ = -3f;
    [Tooltip("World Y of the player when standing at nearZ.")]
    public float nearY = 0f;
    [Tooltip("World Y of the player when standing at farZ.")]
    public float farY = 3f;
    [Tooltip("If true, the player is gently pulled back onto the X position they entered the stairs at.")]
    public bool lockXToEntryPoint = true;

    /// <summary>Returns the world Y the player should be at for a given Z.</summary>
    public float GetYForZ(float z)
    {
        // Guard against degenerate setup
        float zRange = farZ - nearZ;
        if (Mathf.Approximately(zRange, 0f)) return nearY;

        float t = Mathf.Clamp01((z - nearZ) / zRange);
        return Mathf.Lerp(nearY, farY, t);
    }

    /// <summary>Clamps a Z value so the player can't walk off either end of the stairs.</summary>
    public float ClampZ(float z)
    {
        float minZ = Mathf.Min(nearZ, farZ);
        float maxZ = Mathf.Max(nearZ, farZ);
        return Mathf.Clamp(z, minZ, maxZ);
    }

    void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.color = type == StairType.Diagonal
            ? new Color(0f, 1f, 1f, 0.4f)
            : new Color(1f, 0f, 1f, 0.4f);

        Gizmos.matrix = transform.localToWorldMatrix;
        BoxCollider box = col as BoxCollider;
        if (box != null) Gizmos.DrawCube(box.center, box.size);

        // Draw the depth slope line so you can see it in the Scene view
        if (type == StairType.Depth)
        {
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.magenta;
            Vector3 a = new Vector3(transform.position.x, nearY, nearZ);
            Vector3 b = new Vector3(transform.position.x, farY, farZ);
            Gizmos.DrawLine(a, b);
        }
    }
}
