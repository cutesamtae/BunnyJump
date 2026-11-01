/*****************************************************************************
 // File Name:         BunnyPlayer.cs
 // Author:            Hongseok Kim
 // Creation Date:     Nov 1, 2026
 //
 // Brief Description: Handles horizontal input, wrapping, and automatic
 //                    platform bouncing.
 *****************************************************************************/
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Moves the bunny and bounces only on the top of a platform.
/// </summary>
public class BunnyPlayer : MonoBehaviour
{
    /// <summary>
    /// Smallest input that changes the facing direction.
    /// </summary>
    private const float InputDeadZone = 0.01f;

    /// <summary>
    /// Tolerance for detecting a descending landing.
    /// </summary>
    private const float LandingSpeedTolerance = 0.1f;

    /// <summary>
    /// Minimum upward contact normal accepted as a landing.
    /// </summary>
    private const float MinimumLandingNormal = 0.5f;

    [Tooltip("Horizontal movement speed in world units per second.")]
    [SerializeField]
    private float moveSpeed = 7f;

    [Tooltip("Upward speed applied after landing on a platform.")]
    [SerializeField]
    private float bounceSpeed = 11f;

    [Tooltip("Camera used for wrapping and checking the visible area.")]
    [SerializeField]
    private Camera gameCamera;

    [Tooltip("Controller that manages score and game over.")]
    [SerializeField]
    private BunnyGameController controller;

    [Tooltip("Sprite displayed while rising.")]
    [SerializeField]
    private Sprite jumpSprite;

    [Tooltip("Sprite displayed while falling.")]
    [SerializeField]
    private Sprite fallSprite;

    /// <summary>
    /// The cached physics body.
    /// </summary>
    private Rigidbody2D rb;

    /// <summary>
    /// The renderer used to change the bunny pose.
    /// </summary>
    private SpriteRenderer spriteRenderer;

    /// <summary>
    /// Horizontal input from the new Input System.
    /// </summary>
    private InputAction move;

    /// <summary>
    /// Vertical speed before the physics solver resolves a landing.
    /// </summary>
    private float approachSpeed;

    /// <summary>
    /// Creates the Move action with keyboard bindings.
    /// </summary>
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        move = new InputAction("Move", InputActionType.Value);
        move.AddCompositeBinding("1DAxis").With("Negative",
            "<Keyboard>/a").With("Positive", "<Keyboard>/d");
        move.AddCompositeBinding("1DAxis").With("Negative",
            "<Keyboard>/leftArrow").With("Positive", "<Keyboard>/rightArrow");
    }

    /// <summary>
    /// Enables movement when the object becomes active.
    /// </summary>
    private void OnEnable()
    {
        move.Enable();
    }

    /// <summary>
    /// Disables input when the object becomes inactive.
    /// </summary>
    private void OnDisable()
    {
        move.Disable();
    }

    /// <summary>
    /// Releases the input action when the object is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        move.Dispose();
    }

    /// <summary>
    /// Applies horizontal movement while preserving vertical physics.
    /// </summary>
    private void FixedUpdate()
    {
        if (controller.IsGameOver)
        {
            return;
        }

        float xMove = move.ReadValue<float>();
        approachSpeed = rb.linearVelocity.y;
        rb.linearVelocity = new Vector2(xMove * moveSpeed, rb.linearVelocity.y);
        // Wrap at the camera edges without changing height or velocity.
        float halfWidth = gameCamera.orthographicSize * gameCamera.aspect;
        Vector2 position = rb.position;
        float center = gameCamera.transform.position.x;
        if (position.x < center - halfWidth)
        {
            position.x += halfWidth * 2f;
        }
        else if (position.x > center + halfWidth)
        {
            position.x -= halfWidth * 2f;
        }

        rb.position = position;
    }

    /// <summary>
    /// Changes the pose and facing direction using the current motion.
    /// </summary>
    private void Update()
    {
        if (controller.IsGameOver)
        {
            return;
        }

        spriteRenderer.sprite = rb.linearVelocity.y > 0f ? jumpSprite :
            fallSprite;
        float xMove = move.ReadValue<float>();
        if (Mathf.Abs(xMove) > InputDeadZone)
        {
            spriteRenderer.flipX = xMove < 0f;
        }
    }

    /// <summary>
    /// Bounces only when falling onto an upward-facing platform contact.
    /// </summary>
    /// <param name="collision">Contact information supplied by Unity.</param>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (controller.IsGameOver ||
            collision.gameObject.GetComponent<BunnyPlatform>() == null)
        {
            return;
        }

        // Use the pre-solver speed because a landing can already have stopped
        // the fall.
        if (approachSpeed > LandingSpeedTolerance)
        {
            return;
        }

        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y > MinimumLandingNormal)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x,
                    bounceSpeed);
                controller.PlayBounce();
                return;
            }
        }
    }
}
