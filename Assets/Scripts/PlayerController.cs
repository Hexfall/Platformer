using UnityEngine;

namespace AGDDPlatformer
{
    public class PlayerController : KinematicObject
    {
        [Header("Movement")]
        public float maxSpeed = 7;
        public float jumpSpeed = 7;
        public float jumpDeceleration = 0.5f; // Upwards slow after releasing jump button
        public float cayoteTime = 0.1f; // Lets player jump just after leaving ground
        public float jumpBufferTime = 0.1f; // Lets the player input a jump just before becoming grounded

        [Header("Dash")]
        public float dashSpeed;
        public float dashTime;
        public float dashCooldown;
        public Color canDashColor;
        public Color cantDashColor;
        float lastDashTime;
        Vector2 dashDirection;
        bool isDashing;
        bool canDash;
        bool wantsToDash;

        [Header("Audio")]
        public AudioSource source;
        public AudioClip jumpSound;
        public AudioClip dashSound;

        Vector2 startPosition;
        bool startOrientation;

        float lastJumpTime;
        float lastGroundedTime;
        bool canJump;
        bool jumpReleased;
        Vector2 move;
        float defaultGravityModifier;

        SpriteRenderer spriteRenderer;

        void Awake()
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            lastJumpTime = -jumpBufferTime * 2;

            startPosition = transform.position;
            startOrientation = spriteRenderer.flipX;

            defaultGravityModifier = gravityModifier;
        }

        void Update()
        {
            isFrozen = GameManager.instance.timeStopped;

            /* --- Read Input --- */

            move.x = Input.GetAxisRaw("Horizontal");
            if (gravityModifier < 0)
            {
                move.x *= -1;
            }

            move.y = Input.GetAxisRaw("Vertical");

            if (Input.GetButtonDown("Jump"))
            {
                // Store jump time so that we can buffer the input
                lastJumpTime = Time.time;
            }

            if (Input.GetButtonUp("Jump"))
            {
                jumpReleased = true;
            }

            // Clamp directional input to 8 directions for dash
            Vector2 desiredDashDirection = new Vector2(
                move.x == 0 ? 0 : (move.x > 0 ? 1 : -1),
                move.y == 0 ? 0 : (move.y > 0 ? 1 : -1));
            if (desiredDashDirection == Vector2.zero)
            {
                // Dash in facing direction if there is no directional input;
                desiredDashDirection = spriteRenderer.flipX ? -Vector2.right : Vector2.right;
            }
            desiredDashDirection = desiredDashDirection.normalized;
            if (Input.GetButtonDown("Dash"))
            {
                wantsToDash = true;
            }

            /* --- Compute Velocity --- */

            if (canDash && wantsToDash)
            {
                isDashing = true;
                dashDirection = desiredDashDirection;
                lastDashTime = Time.time;
                canDash = false;
                gravityModifier = 0;

                source.PlayOneShot(dashSound);
            }
            wantsToDash = false;

            if (isDashing)
            {
                velocity = dashDirection * dashSpeed;
                if (Time.time - lastDashTime >= dashTime)
                {
                    isDashing = false;
                    
                    gravityModifier = defaultGravityModifier;
                    if ((gravityModifier >= 0 && velocity.y > 0) ||
                        (gravityModifier < 0 && velocity.y < 0))
                    {
                        velocity.y *= jumpDeceleration;
                    }
                }
            }
            else
            {
                if (isGrounded)
                {
                    // Store grounded time to allow for late jumps
                    lastGroundedTime = Time.time;
                    canJump = true;
                    if (!isDashing && Time.time - lastDashTime >= dashCooldown)
                        canDash = true;
                }

                // Check time for buffered jumps and late jumps
                float timeSinceJumpInput = Time.time - lastJumpTime;
                float timeSinceLastGrounded = Time.time - lastGroundedTime;

                if (canJump && timeSinceJumpInput <= jumpBufferTime && timeSinceLastGrounded <= cayoteTime)
                {
                    velocity.y = Mathf.Sign(gravityModifier) * jumpSpeed;
                    canJump = false;
                    isGrounded = false;
                    
                    source.PlayOneShot(jumpSound);
                }
                else if (jumpReleased)
                {
                    // Decelerate upwards velocity when jump button is released
                    if ((gravityModifier >= 0 && velocity.y > 0) ||
                        (gravityModifier < 0 && velocity.y < 0))
                    {
                        velocity.y *= jumpDeceleration;
                    }
                    jumpReleased = false;
                }

                velocity.x = move.x * maxSpeed;
            }

            /* --- Adjust Sprite --- */

            // Assume the sprite is facing right, flip it if moving left
            if (move.x > 0.01f)
            {
                spriteRenderer.flipX = false;
            }
            else if (move.x < -0.01f)
            {
                spriteRenderer.flipX = true;
            }

            spriteRenderer.color = canDash ? canDashColor : cantDashColor;
        }

        public void ResetPlayer()
        {
            transform.position = startPosition;
            spriteRenderer.flipX = startOrientation;

            lastJumpTime = -jumpBufferTime * 2;

            velocity = Vector2.zero;
        }

        public void ResetDash()
        {
            canDash = true;
        }
    }
}
