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
        public float terminalVelocity = 2;

        [Header("Death")]
        public float respawnTime = 2.0f;
        float deathTime;
        bool isDead = false;
        public float KillY;

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
        bool isStuck = false;

        SpriteRenderer spriteRenderer;

        float overrideGravity;

        void Awake()
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            lastJumpTime = -jumpBufferTime * 2;

            startPosition = transform.position;
            startOrientation = spriteRenderer.flipX;

            defaultGravityModifier = gravityModifier;
            overrideGravity = defaultGravityModifier;
        }

        public bool CanDash()
        {
            return canDash;
        }

        void Update()
        {
            if (isDead)
            {
                if (deathTime + respawnTime <= Time.time)
                    ResetPlayer();
                return;
            }

            if (transform.position.y <= KillY)
                Kill();

            isFrozen = GameManager.instance.timeStopped;

            /* --- Read Input --- */

            move.x = Input.GetAxisRaw("Horizontal");
            //if (gravityModifier < 0)
            //{
            //    move.x *= -1;
            //}

            move.y = Input.GetAxisRaw("Vertical");

            if (!isDashing && Mathf.Abs(velocity.y) > terminalVelocity)
                velocity.y = Mathf.Sign(velocity.y) * terminalVelocity;

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
                //desiredDashDirection = spriteRenderer.flipX ? -Vector2.right : Vector2.right;
                // Default dash direction is (relative) up.
                desiredDashDirection = Vector2.up * (gravityModifier < 0 ? -1 : 1);
            }
            //desiredDashDirection = desiredDashDirection.normalized;
            // If player is attempting to jump mid-air, dash.
            if (Input.GetButtonDown("Jump") && (!isGrounded || isStuck))
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
                SetGravity(0);
                Unstick();

                source.PlayOneShot(dashSound);
            }
            wantsToDash = false;

            if (isDashing)
            {
                velocity = dashDirection * dashSpeed;
                if (Time.time - lastDashTime >= dashTime)
                {
                    isDashing = false;
                    
                    ResetGravity();
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

                if (canJump && timeSinceJumpInput <= jumpBufferTime && timeSinceLastGrounded <= cayoteTime && !isStuck)
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

                if (!IsStuck()) velocity.x = move.x * maxSpeed;
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
            if (IsStuck()) velocity = Vector2.zero;
        }

        public void ResetPlayer()
        {
            transform.position = startPosition;
            spriteRenderer.flipX = startOrientation;

            lastJumpTime = -jumpBufferTime * 2;

            velocity = Vector2.zero;
            isDead = false;
            Unstick();
        }

        public void ResetDash()
        {
            canDash = true;
        }

        public void Stick()
        {
            SetGravity(0);
            velocity = Vector2.zero;
            canJump = false;
            isDashing = false;
            isStuck = true;
        }

        public void Unstick()
        {
            ResetGravity();
            isStuck = false;
        }

        public bool IsStuck()
        {
            return isStuck && Input.GetButton("Stick");
        }

        public void Kill()
        {
            isDead = true;
            deathTime = Time.time;
            Stick();
        }

        public void SetGravity(float value)
        {
            gravityModifier = value;
        }

        public void ResetGravity()
        {
            SetGravity(overrideGravity);
        }

        public void ResetGravityHard()
        {
            overrideGravity = defaultGravityModifier;
            ResetGravity();
        }

        public void SetGravityHard(float value)
        {
            overrideGravity = value;
            ResetGravity();
        }

        public bool MovingAgainstGravity()
        {
            return (Mathf.Sign(gravityModifier) != Mathf.Sign(velocity.y));
        }
    }
}
