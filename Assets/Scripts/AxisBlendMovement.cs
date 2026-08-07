using UnityEngine;

/// <summary>
/// Math stuff for movement
/// calculates the blended velocity vectors for the side scrolling movement,
/// somewhat free depth motion forground background crossing, and locked-paths
/// and motion diagonal transitions liek teh STAIRRS
///
/// This class has not a MonoBehaviour state of its own; it is a field on the controller so its tuning vars show up in the Inspector.
/// </summary>
[System.Serializable]
public class AxisBlendMovement
{
    [Header("Lateral Axis (side-scrolling)")]
    [Tooltip("Max movement speed along the lateral (X) axis.")]
    public float lateralSpeed = 5f;

    [Header("Depth Axis (foreground/background)")]
    [Tooltip("Max movement speed along the depth (Z) axis when inside an open depth zone.")]
    public float depthSpeed = 3f;

    [Header("Locked Path (stairs / fixed diagonal transitions)")]
    [Tooltip("Normalized progress-per-second applied along a locked traversal path.")]
    public float pathTraversalSpeed = 0.6f;

    [Header("Smoothing")]
    [Tooltip("Approx. time to reach target horizontal velocity. Lower = snappier.")]
    public float velocitySmoothTime = 0.08f;

    [Tooltip("Approx. time to blend the depth axis in/out when entering or leaving a depth zone.")]
    public float depthBlendSmoothTime = 0.15f;

    // inside smoothing (*insert explanation here or sum*).
    private Vector3 _velocityRef;
    private float _depthBlendRef;

    /// <summary>
    /// Current lerp weight (0..1) of how active depth input is.
    /// 0 = lateral run, 1 = full depth control is available.
    /// </summary>
    public float DepthBlend { get; private set; }

    /// <summary>
    /// Smoothly moves the DepthBlend toward a target (calls it once per frame).
    /// </summary>
    public void UpdateDepthBlend(bool depthInputAllowed, float deltaTime)
    {
        float target = depthInputAllowed ? 1f : 0f;
        DepthBlend = Mathf.SmoothDamp(DepthBlend, target, ref _depthBlendRef, depthBlendSmoothTime, Mathf.Infinity, deltaTime);
    }

    /// <summary>
    /// Chalculates a horizontal+depth velocity for free run or open depth zone moving.
    /// verticalInput drives the Z (depth) axis and is scaled by the current DepthBlend so
    /// depth control should fade in/out a tad smooth rather than snapping when crossing zone bounds.
    /// </summary>
    public Vector3 ComputeFreeVelocity(float horizontalInput, float verticalInput, Vector3 currentVelocity, float deltaTime)
    {
        Vector3 target = new Vector3(
            horizontalInput * lateralSpeed,
            0f,
            verticalInput * depthSpeed * DepthBlend
        );

        Vector3 smoothed = Vector3.SmoothDamp(currentVelocity, target, ref _velocityRef, velocitySmoothTime, Mathf.Infinity, deltaTime);
        return smoothed;
    }

    public float AdvancePathProgress(float currentT, float directionalInput, float deltaTime)
    {
        return Mathf.Clamp01(currentT + directionalInput * pathTraversalSpeed * deltaTime);
    }

    /// <summary>
    /// Resets internal smoothing refs, useful when the controller is teleported
    /// or a path transition completes, to avoid velocity Explosions.
    /// </summary>
    public void ResetSmoothing()
    {
        _velocityRef = Vector3.zero;
        _depthBlendRef = 0f;
    }
}
