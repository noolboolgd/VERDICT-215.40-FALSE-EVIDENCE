using UnityEngine;

/// <summary>
///I got lazy
/// </summary>
public class DepthVisualSync : MonoBehaviour
{
    [Header("Depth Range (should match your DepthZoneVolume ranges)")]
    public float nearDepth = -2f;
    public float farDepth = 2f;

    [Header("Scale")]
    public float nearScale = 1.1f;
    public float farScale = 0.85f;

    [Header("Sorting")]
    public SpriteRenderer[] sortedRenderers;
    public int nearSortingOrder = 100;
    public int farSortingOrder = 0;

    [Tooltip("Smoothing time for scale changes so lane shifts don't pop.")]
    public float scaleSmoothTime = 0.2f;

    private float _scaleVelocity;
    private float _currentScale;

    private void Start()
    {
        _currentScale = transform.localScale.x;
    }

    private void LateUpdate()
    {
        float depth01 = Mathf.InverseLerp(nearDepth, farDepth, transform.position.z);

        float targetScale = Mathf.Lerp(nearScale, farScale, depth01);
        _currentScale = Mathf.SmoothDamp(_currentScale, targetScale, ref _scaleVelocity, scaleSmoothTime);

        Vector3 scale = transform.localScale;
        float sign = Mathf.Sign(scale.x == 0f ? 1f : scale.x);
        transform.localScale = new Vector3(sign * _currentScale, _currentScale, scale.z);

        int order = Mathf.RoundToInt(Mathf.Lerp(nearSortingOrder, farSortingOrder, depth01));
        if (sortedRenderers != null)
        {
            for (int i = 0; i < sortedRenderers.Length; i++)
            {
                if (sortedRenderers[i] != null)
                {
                    sortedRenderers[i].sortingOrder = order;
                }
            }
        }
    }
}
