using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    // Constants
    // const float framesPerSecond = 60;
    // Public values
    public float inputBufferDurationFrames;
    public float movementSpeed;
    public float movementAccel;
    public float jumpHeight;
    public float dashDistance;
    public float dashSpeed;
    public float dashCooldown;
    public float maxGroundAngle;
    public float maxWallAngle;
    // Components
    Rigidbody2D _rigidbody;
    // Time tracking
    float currentTime;
    // Saved input values
    enum BufferedInput
    {
        NONE,
        JUMP,
        DASH
    };
    BufferedInput bufferedInput = BufferedInput.NONE;
    float bufferedTimestamp; // timestamp at which we buffered the last input
    Vector2 moveInput;
    // Movement state data
    enum MovementState
    {
        NORMAL,
        JUMPING,
        DASHING
    };
    MovementState movementState;
    float stateTimestamp; // timestamp at which we entered the current state
    int groundCount = 0; // number of ground objects we are in contact with
    int wallCount = 0; // number of wall objects we are in contact with
    bool hasDash = true;
    float dashTimestamp = 0;
    float dashDirection;
    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
    }
    // Start is called before the first frame update
    void Start()
    {
        currentTime = 0;
    }

    // Update is called once per frame
    void Update()
    {
        // Update timekeeping
        currentTime += Time.deltaTime;

        // Update movement state
        if (movementState != MovementState.DASHING &&
            currentTime - dashTimestamp >= dashCooldown &&
            bufferedInput == BufferedInput.DASH)
        {
            // If our dash cooldown is over and we buffered a dash, process it
            bufferedInput = BufferedInput.NONE;
            startDash();
        }

        // Change rigidbody velocity based on movement state
        move();
    }

    void move()
    {
        Vector2 oldVel = _rigidbody.velocity;
        Vector2 newVel = new Vector2(oldVel.x, oldVel.y);

        float stateElapsedTime = currentTime - stateTimestamp;

        switch (movementState)
        {
            case MovementState.JUMPING:
                float jumpDuration = Mathf.Sqrt(-2 * jumpHeight /
                    Physics2D.gravity.y);
                if (stateElapsedTime >= jumpDuration)
                {
                    // Finish jump
                    setMovementState(MovementState.NORMAL);
                }
                // Debug.Log(newVel.y + ", " + stateElapsedFrames);
                goto case MovementState.NORMAL; // fall through
            case MovementState.NORMAL:
                float xVelMax = Mathf.Abs(moveInput.x * movementSpeed);
                float xVelUncapped = oldVel.x + Mathf.Sign(moveInput.x) * movementAccel * Time.deltaTime;
                float xVel = Mathf.Max(-xVelMax, Mathf.Min(xVelMax, xVelUncapped));

                newVel.x = xVel;
                break;
            case MovementState.DASHING:
                float dashDuration = dashDistance / dashSpeed;
                if (stateElapsedTime > dashDuration)
                {
                    // Finish dash
                    _rigidbody.gravityScale = 1;
                    if (IsGrounded()) hasDash = true;
                    dashTimestamp = currentTime;
                    setMovementState(MovementState.NORMAL);
                    return;
                }

                newVel = getDashVelocity(dashDirection, stateElapsedTime);
                break;
            default:
                Debug.LogError("Unknown movement state!");
                newVel = new Vector2(0, 0);
                break;
        }

        _rigidbody.velocity = newVel;
    }

    Vector2 getDashVelocity(float dashDirection, float stateElapsedFrames)
    {
        return new Vector2(dashDirection * dashSpeed, 0);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            // Button down
            if (movementState == MovementState.NORMAL && IsGrounded())
            {
                startJump();
            }
            else
            {
                bufferInput(BufferedInput.JUMP);
            }
        }
        else if (context.canceled)
        {
            // Button up
            endJump();
        }
    }

    void startJump()
    {
        // Debug.Log("Start jump");
        // Set Y velocity to min jump speed
        float jumpSpeedCorrection = -0.5f * Time.fixedDeltaTime * Physics2D.gravity.y; // Compensate for changing velocity directly
        float jumpSpeed = Mathf.Sqrt(-2 *
            Physics2D.gravity.y *
            jumpHeight) +
            jumpSpeedCorrection;
        _rigidbody.velocity = new Vector2(_rigidbody.velocity.x, jumpSpeed);
        // jumpReleaseBuffered = false;
        setMovementState(MovementState.JUMPING);
    }

    void endJump()
    {
        if (movementState == MovementState.JUMPING)
        {
            // Zero out velocity
            _rigidbody.velocity = new Vector2(_rigidbody.velocity.x, 0);
            setMovementState(MovementState.NORMAL);
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            // Button down
            startDash();
        }
    }
    
    void startDash()
    {
        if (movementState == MovementState.NORMAL && hasDash
            && currentTime - dashTimestamp >= dashCooldown)
        {
            dashDirection = Mathf.Sign(moveInput.x);
            _rigidbody.gravityScale = 0;
            hasDash = false;
            setMovementState(MovementState.DASHING);
        }
        else
        {
            bufferInput(BufferedInput.DASH);
        }
    }

    /// <summary>
    /// Sent when an incoming collider makes contact with this object's
    /// collider (2D physics only).
    /// </summary>
    /// <param name="other">The Collision2D data associated with this collision.</param>
    void OnCollisionEnter2D(Collision2D other)
    {
        detectTerrainContacts(other);
    }

    /// <summary>
    /// Sent each frame where a collider on another object is touching
    /// this object's collider (2D physics only).
    /// </summary>
    /// <param name="other">The Collision2D data associated with this collision.</param>
    void OnCollisionStay2D(Collision2D other)
    {
        detectTerrainContacts(other);
    }

    /// <summary>
    /// Sent when a collider on another object stops touching this
    /// object's collider (2D physics only).
    /// </summary>
    /// <param name="other">The Collision2D data associated with this collision.</param>
    void OnCollisionExit2D(Collision2D other)
    {
        LevelTerrain terrain = other.gameObject.GetComponent<LevelTerrain>();
        if (terrain != null)
        {
            updateContactCounts(terrain.RemoveGroundContact(this), terrain.RemoveWallContact(this));
        }
    }

    void detectTerrainContacts(Collision2D other)
    {
        LevelTerrain terrain = other.gameObject.GetComponent<LevelTerrain>();
        if (terrain != null)
        {
            bool groundContact = false;
            bool wallContact = false;

            // Iterate over every normal in the collision
            foreach (var item in other.contacts)
            {
                // Detect ground and wall contacts
                if (Vector2.Angle(item.normal, new Vector2(0, 1)) < maxGroundAngle)
                {
                    groundContact = true;
                }
                if (Vector2.Angle(item.normal, new Vector2(1, 0)) < maxWallAngle ||
                    Vector2.Angle(item.normal, new Vector2(-1, 0)) < maxWallAngle)
                {
                    wallContact = true;
                }
                // Debug.DrawRay(item.point, item.normal * 100, Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f), 10f);
            }

            // Update ground and wall contact counts
            int groundIncrement;
            int wallIncrement;
            if (groundContact)
            {
                groundIncrement = terrain.AddGroundContact(this);
            }
            else
            {
                groundIncrement = terrain.RemoveGroundContact(this);
            }
            if (wallContact)
            {
                wallIncrement = terrain.AddWallContact(this);
            }
            else
            {
                wallIncrement = terrain.RemoveWallContact(this);
            }
            updateContactCounts(groundIncrement, wallIncrement);
        }
    }

    void setMovementState(MovementState state)
    {
        movementState = state;
        stateTimestamp = currentTime;

        // Process buffered input if we returned to normal state
        if (state == MovementState.NORMAL)
        {
            processInputBuffer();
        }
    }

    void processInputBuffer()
    {
        if (currentTime - bufferedTimestamp <= inputBufferDurationFrames)
        {
            // Debug.Log("Processing buffered input " + bufferedInput);
            switch (bufferedInput)
            {
                case BufferedInput.JUMP:
                    if (movementState == MovementState.NORMAL && IsGrounded()) startJump();
                    break;
                case BufferedInput.DASH:
                    startDash();
                    break;
            }
            bufferedInput = BufferedInput.NONE;
        }
        else
        {
            // Debug.Log("buffer expired");
        }
    }

    void bufferInput(BufferedInput input)
    {
        bufferedInput = input;
        bufferedTimestamp = currentTime;
        // Debug.Log("Buffered input " + input);
    }

    void updateContactCounts(int groundIncrement, int wallIncrement)
    {
        bool wasGrounded = IsGrounded();
        bool wasOnWall = IsOnWall();
        groundCount += groundIncrement;
        wallCount += wallIncrement;
        Debug.Assert(groundCount >= 0);
        Debug.Assert(wallCount >= 0);

        if (IsGrounded() && !wasGrounded)
        {
            // Debug.Log("grounded");
            hasDash = true;
            if (movementState == MovementState.JUMPING) setMovementState(MovementState.NORMAL);
            processInputBuffer();
        }
        else if (IsOnWall() && !wasOnWall)
        {
            // Debug.Log("on wall");
            hasDash = true;
            if (movementState == MovementState.JUMPING) setMovementState(MovementState.NORMAL);
            processInputBuffer();
        }
        else if (!IsGrounded() && !IsOnWall())
        {
            if (wasGrounded)
            {
                // Debug.Log("free");
            }
            else if (wasOnWall)
            {

            }
        }
    }

    public bool IsGrounded()
    {
        return groundCount > 0;
    }
    public bool IsOnWall()
    {
        return wallCount > 0 && groundCount == 0;
    }
}
