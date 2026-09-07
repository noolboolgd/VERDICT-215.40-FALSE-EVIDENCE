using UnityEngine;

/// <summary>
/// Marks a trigger volume as a staircase. Place this on a BoxCollider (isTrigger = true)
/// that covers the stair area
/// </summary>
[RequireComponent(typeof(Collider))]
public class StairZone : MonoBehaviour
{
    public enum StairType
    {
        Diagonal,
        Depth
    }

    [Tooltip("Diagonal = slanted stairs seen from the side. Depth = stairs that go into/out of the screen.")]
    public StairType type = StairType.Diagonal;

    [Header("Diagonal Stairs Settings")]
    [Tooltip("Direction the stairs rise, e.g. (1,1) for up-to-the-right, (1,-1) for down-to-the-right.")]
    public Vector2 slopeDirection = new Vector2(1, 1);

    [Header("Depth Stairs Settings")]
    [Tooltip("World Z of the bottom of the stairs (closest to camera).")]
    public float nearZ = 0f;
    [Tooltip("World Z of the top of the stairs (furthest from camera).")]
    public float farZ = -3f;
    [Tooltip("If true, the player is gently pulled back onto the X position they entered the stairs at.")]
    public bool lockXToEntryPoint = true;

    void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.color = type == StairType.Diagonal ? new Color(0f, 1f, 1f, 0.4f) : new Color(1f, 0f, 1f, 0.4f);
        Gizmos.matrix = transform.localToWorldMatrix;
        BoxCollider box = col as BoxCollider;
        if (box != null)
        {
            Gizmos.DrawCube(box.center, box.size);
        }
    }
}