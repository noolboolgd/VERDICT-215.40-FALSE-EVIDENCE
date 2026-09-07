using UnityEngine;

/// <summary>
/// Updatest hhe SpriteRenderer's sortingOrder based on world Z (and Y as a tiebreaker)
/// so that characters/objects further "into" the screen draw behind ones closer to
/// Attach to any sprite that moves between lanes or stairs (the player, NPCs, movable props).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteDepthSorter : MonoBehaviour
{
    [Tooltip("How strongly Z position affects sort order. Higher = more separation between lanes.")]
    public float zSortFactor = 100f;
    [Tooltip("How strongly Y position affects sort order within the same lane (higher up sorts behind).")]
    public float ySortFactor = 10f;

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        sr.sortingOrder = Mathf.RoundToInt(-transform.position.z * zSortFactor - transform.position.y * ySortFactor);
    }
}