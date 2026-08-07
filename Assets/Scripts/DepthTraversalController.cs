using UnityEngine;
/// https://codebeautify.org/csharpviewer
/// <summary> (We need comment highlight fr fr)
/// The character controller with:
///   flat side-scrolling movement along the (X) axis.
///   somewhat free depth-blending inside DepthZoneVolume triggers, letting the player
///    shift between foreground/background lanes.
///  Locked diagonal transitions (stairs, ramps) via TraversalPathZone triggers,
///    where the character walks a fixed parametric path instead of free-blending.
///
/// Uses the builtin charactercontroller for gravity and collision stuff,
/// so it should work nicely on both flat and angled colliders typically
/// for STAIIRRSS.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class DepthTraversalController : MonoBehaviour
{
    private enum TraversalMode
    {
        FreeRun,     // flatish lateral side-scrolling, no depth input
        OpenDepth,   // inside a *DepthZoneVolume*, lateral + depth both free
        LockedPath   // inside a *TraversalPathZone*, following a fixed diagonal route
    }

    [Header("Input")]
    public string horizontalAxis = "Horizontal";
    public string verticalAxis = "Vertical";
    public KeyCode jumpKey = KeyCode.Space;

    [Header("Movement Tuning")]
    public AxisBlendMovement movement = new AxisBlendMovement();

    [Header("Gravity & Jump")]
    public float gravity = -25f;
    public float jumpHeight = 1.4f;
    [Tooltip("Small downward force applied while grounded to keep the CharacterController grounded reliably.")]
    public float groundStickForce = -2f;

    [Header("Facing")]
    [Tooltip("Root/visual transform to flip on the X axis when lateral direction changes. Leave empty to skip.")]
    public Transform visualRoot;

    private CharacterController _controller;
    private TraversalMode _mode = TraversalMode.FreeRun;

    private Vector3 _horizontalVelocity;   // X/Z velocity, smoothed by AxisBlendMovement
    private float _verticalVelocity;       // Y velocity (gravity/jump), handled separately

    // Open depth zone state
    private DepthZoneVolume _activeDepthZone;

    // Locked path state
    private TraversalPathZone _activePath;
    private float _pathT;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        float h = Input.GetAxisRaw(horizontalAxis);
        float v = Input.GetAxisRaw(verticalAxis);

        switch (_mode)
        {
            case TraversalMode.FreeRun:
            case TraversalMode.OpenDepth:
                TickFreeMovement(h, v, dt);
                break;

            case TraversalMode.LockedPath:
                TickLockedPath(v, dt);
                break;
        }

        TickGravityAndJump(dt);
        UpdateFacing(h);

        // Combine horizontal + vertical into a single Move call so collisions
        // are resolved consistently for both flat runs and angled stair paths.
        Vector3 delta = (_horizontalVelocity + Vector3.up * _verticalVelocity) * dt;
        _controller.Move(delta);
    }

    // ------------------------------------------------------------------
    // Free-run / open-depth movement
    // ------------------------------------------------------------------

    private void TickFreeMovement(float h, float v, float dt)
    {
        bool depthAllowed = _mode == TraversalMode.OpenDepth;
        movement.UpdateDepthBlend(depthAllowed, dt);

        Vector3 target = movement.ComputeFreeVelocity(h, v, _horizontalVelocity, dt);

        if (depthAllowed && _activeDepthZone != null)
        {
            // Predict next Z and clamp it to the zone's allowed depth range so
            // the player can't blend past the edges of the current lane band.
            float predictedZ = transform.position.z + target.z * dt;
            float clampedZ = _activeDepthZone.ClampDepth(predictedZ);
            if (!Mathf.Approximately(clampedZ, predictedZ))
            {
                target.z = 0f; // hit the depth boundary; stop further depth push
            }
        }

        _horizontalVelocity = new Vector3(target.x, 0f, target.z);
    }

    // ------------------------------------------------------------------
    // Locked diagonal path movement (stairs / scripted transitions)
    // ------------------------------------------------------------------

    private void TickLockedPath(float directionalInput, float dt)
    {
        if (_activePath == null)
        {
            _mode = TraversalMode.FreeRun;
            return;
        }

        _pathT = movement.AdvancePathProgress(_pathT, directionalInput, dt);
        Vector3 targetPoint = _activePath.GetPointAtT(_pathT);

        // Drive horizontal velocity implicitly by comparing the desired path
        // point to the current position, so CharacterController.Move still
        // performs proper collision resolution rather than teleporting.
        Vector3 toTarget = targetPoint - transform.position;
        toTarget.y = 0f; // vertical component is handled by gravity/step-up below
        _horizontalVelocity = toTarget / Mathf.Max(dt, 0.0001f);

        // Height along the path is applied directly since stairs typically need
        // exact step alignment rather than gravity-simulated climbing.
        float heightDelta = targetPoint.y - transform.position.y;
        _controller.Move(new Vector3(0f, heightDelta, 0f));
        _verticalVelocity = 0f;

        if (_activePath.autoReleaseAtEnds && (_pathT <= 0f || _pathT >= 1f))
        {
            ExitPath(_activePath);
        }
    }

    // ------------------------------------------------------------------
    // Gravity / jump (skipped while locked to a path, since height is
    // driven explicitly by the path in that mode)
    // ------------------------------------------------------------------

    private void TickGravityAndJump(float dt)
    {
        if (_mode == TraversalMode.LockedPath)
        {
            return;
        }

        if (_controller.isGrounded)
        {
            if (_verticalVelocity < 0f)
            {
                _verticalVelocity = groundStickForce;
            }

            if (Input.GetKeyDown(jumpKey))
            {
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
        else
        {
            _verticalVelocity += gravity * dt;
        }
    }

    private void UpdateFacing(float h)
    {
        if (visualRoot == null || Mathf.Abs(h) < 0.01f)
        {
            return;
        }

        Vector3 scale = visualRoot.localScale;
        scale.x = Mathf.Sign(h) * Mathf.Abs(scale.x);
        visualRoot.localScale = scale;
    }

    // ------------------------------------------------------------------
    // Zone transition callbacks (claled by DepthZoneVolume / TraversalPathZone)
    // ------------------------------------------------------------------

    public void EnterDepthZone(DepthZoneVolume zone)
    {
        if (_mode == TraversalMode.LockedPath)
        {
            return; // a locked path takes priority; ignore overlapping depth zones
        }

        _activeDepthZone = zone;
        _mode = TraversalMode.OpenDepth;
    }

    public void ExitDepthZone(DepthZoneVolume zone)
    {
        if (_activeDepthZone != zone)
        {
            return;
        }

        _activeDepthZone = null;
        if (_mode == TraversalMode.OpenDepth)
        {
            _mode = TraversalMode.FreeRun;
        }
    }

    public void EnterPath(TraversalPathZone path)
    {
        _activePath = path;
        _pathT = path.GetClosestT(transform.position);
        movement.ResetSmoothing();
        _mode = TraversalMode.LockedPath;
    }

    public void ExitPath(TraversalPathZone path)
    {
        if (_activePath != path)
        {
            return;
        }

        _activePath = null;
        movement.ResetSmoothing();
        _mode = _activeDepthZone != null ? TraversalMode.OpenDepth : TraversalMode.FreeRun;
    }
}
