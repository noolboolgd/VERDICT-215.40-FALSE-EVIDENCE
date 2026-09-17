using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController2_5D : MonoBehaviour
{
    public enum FacingDirection { Right, Left, Away, Toward }

    [Header("Ground Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float crouchSpeed = 1.5f;

    [Header("Stairs")]
    [Tooltip("Multiplier applied to walk speed while on diagonal (side-view) stairs.")]
    public float diagonalStairSpeedMultiplier = 0.6f;
    [Tooltip("Multiplier applied to walk speed while moving along depth (toward/away camera) stairs.")]
    public float depthStairSpeedMultiplier = 0.7f;
    [Tooltip("If true, holding Run also speeds up diagonal stairs (still capped below normal run speed).")]
    public bool allowRunOnStairs = false;

    [Header("Crouch")]
    public float standingHeight = 2f;
    public float crouchingHeight = 1f;
    public Vector3 standingCenter = new Vector3(0, 1f, 0);
    public Vector3 crouchingCenter = new Vector3(0, 0.5f, 0);

    [Header("Gravity")]
    public float gravity = -20f;
    public float groundedStickForce = -1f;

    [Header("Input")]
    public KeyCode runKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;

    [Header("References")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    [Tooltip("Optional, only used if no Animator is assigned - swaps sprite directly when facing the camera.")]
    public Sprite towardCameraSprite;
    [Tooltip("Optional, only used if no Animator is assigned - swaps sprite directly when facing away from the camera.")]
    public Sprite awayFromCameraSprite;

    private Sprite defaultSprite;
    private CharacterController controller;
    private StairZone currentStair;
    private float verticalVelocity;
    private bool isCrouching;
    private bool isRunning;
    private FacingDirection facing = FacingDirection.Right;
    private float depthRailX; // x position locked onto while traversing a depth stair

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) defaultSprite = spriteRenderer.sprite;
    }

    void Update()
{
    float h = Input.GetAxisRaw("Horizontal");
    float v = Input.GetAxisRaw("Vertical");
    isRunning = Input.GetKey(runKey);
    bool crouchHeld = Input.GetKey(crouchKey);

    bool onDiagonalStairs = currentStair != null && currentStair.type == StairZone.StairType.Diagonal;
    bool onDepthStairs    = currentStair != null && currentStair.type == StairZone.StairType.Depth;

    Vector3 velocity = Vector3.zero;

    if (onDepthStairs)
    {
        isCrouching = false;
        float speed = walkSpeed * depthStairSpeedMultiplier;
        velocity.z = v * speed;

        if (currentStair.lockXToEntryPoint)
        {
            float xDiff = depthRailX - transform.position.x;
            velocity.x = Mathf.Clamp(xDiff * 10f, -speed, speed);
        }

        if (v > 0.01f) facing = FacingDirection.Away;
        else if (v < -0.01f) facing = FacingDirection.Toward;
    }
    else if (onDiagonalStairs)
    {
        isCrouching = false;
        float speed = walkSpeed * diagonalStairSpeedMultiplier;
        if (allowRunOnStairs && isRunning) speed = runSpeed * diagonalStairSpeedMultiplier;

        Vector2 dir = currentStair.slopeDirection.sqrMagnitude > 0.0001f
            ? currentStair.slopeDirection.normalized
            : Vector2.right;

        velocity.x = dir.x * h * speed;
        velocity.y = dir.y * Mathf.Abs(h) * speed;

        if (h > 0.01f) facing = FacingDirection.Right;
        else if (h < -0.01f) facing = FacingDirection.Left;
    }
    else
    {
        isCrouching = crouchHeld;
        float speed = isCrouching ? crouchSpeed : (isRunning ? runSpeed : walkSpeed);
        velocity.x = h * speed;

        if (h > 0.01f) facing = FacingDirection.Right;
        else if (h < -0.01f) facing = FacingDirection.Left;
    }

    
    if (onDepthStairs)
    {
        
        verticalVelocity = 0f;
    }
    else if (controller.isGrounded)
    {
        verticalVelocity = groundedStickForce;
    }
    else if (!onDiagonalStairs)
    {
        verticalVelocity += gravity * Time.deltaTime;
    }

    if (!onDiagonalStairs && !onDepthStairs)
    {
        velocity.y = verticalVelocity;
    }

    // --- Move ---
    controller.Move(velocity * Time.deltaTime);

    
    if (onDepthStairs)
    {
        Vector3 pos = transform.position;

        // clamp Z so the player can't walk off either end of the trigger.
        float clampedZ = currentStair.ClampZ(pos.z);

        // drive Y from the stair slope for the (possibly clamped) Z.
        float targetY = currentStair.GetYForZ(clampedZ);

        // Move through controller so collisions still work, but ignore the
        // normal gravity thingy Ive skipped up.
        Vector3 correction = new Vector3(0f, targetY - pos.y, clampedZ - pos.z);
        controller.Move(correction);

        // Re-anchor vertical velocity to 0 so we don't accumulate falling.
        verticalVelocity = 0f;
    }

    UpdateCrouchCollider();
    UpdateFacing();
    UpdateAnimator(h, v, onDiagonalStairs, onDepthStairs);
}
    void UpdateCrouchCollider()
    {
        float targetHeight = isCrouching ? crouchingHeight : standingHeight;
        Vector3 targetCenter = isCrouching ? crouchingCenter : standingCenter;
        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * 10f);
        controller.center = Vector3.Lerp(controller.center, targetCenter, Time.deltaTime * 10f);
    }

    void UpdateFacing()
    {
        if (spriteRenderer == null) return;

        if (facing == FacingDirection.Left || facing == FacingDirection.Right)
        {
            spriteRenderer.flipX = facing == FacingDirection.Left;
            if (animator == null && defaultSprite != null) spriteRenderer.sprite = defaultSprite;
        }
        else if (animator == null)
        {
            // Fallback direct sprite swap when there's no Animator driving this.
            if (facing == FacingDirection.Toward && towardCameraSprite != null)
                spriteRenderer.sprite = towardCameraSprite;
            else if (facing == FacingDirection.Away && awayFromCameraSprite != null)
                spriteRenderer.sprite = awayFromCameraSprite;
        }
    }

    void UpdateAnimator(float h, float v, bool onDiagonalStairs, bool onDepthStairs)
    {
        if (animator == null) return;

        float normalizedSpeed = onDepthStairs ? Mathf.Abs(v) : Mathf.Abs(h);
        animator.SetFloat("Speed", normalizedSpeed);
        animator.SetBool("IsCrouching", isCrouching);
        animator.SetBool("IsRunning", isRunning && !onDiagonalStairs && !onDepthStairs);
        animator.SetBool("OnStairsDiagonal", onDiagonalStairs);
        animator.SetBool("OnStairsDepth", onDepthStairs);
        animator.SetInteger("FacingDirection", (int)facing);
    }

    void OnTriggerEnter(Collider other)
    {
        StairZone stair = other.GetComponent<StairZone>();
        if (stair != null)
        {
            currentStair = stair;
            depthRailX = transform.position.x;
        }
    }

    void OnTriggerExit(Collider other)
    {
        StairZone stair = other.GetComponent<StairZone>();
        if (stair != null && stair == currentStair)
        {
            currentStair = null;
        }
    }
}
