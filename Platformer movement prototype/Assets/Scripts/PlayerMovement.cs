using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    // Public values
    public float inputBufferDuration;
    public float movementSpeed;
    public float movementAccel;
    public float jumpSpeed;
    public float dashDistance;
    public float dashSpeed;
    // Components
    Rigidbody2D _rigidbody;
    // Time tracking
    float deltaFrames;
    float currentTime;
    // Saved input values
    enum BufferedInput {
        JUMP,
        DASH
    };
    BufferedInput bufferedInput;
    float bufferedTimestamp; // timestamp at which we buffered the last input
    Vector2 moveInput;
    // Movement state data
    enum MovementState {
        NORMAL,
        DASHING
    };
    MovementState movementState;
    float stateTimestamp; // timestamp at which we entered the current state
    bool grounded;
    bool hasDash;
    float dashDirection;
    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        deltaFrames = Time.deltaTime * 60;
        currentTime += deltaFrames;
        move();
    }

    void move()
    {
        Vector2 oldVel = _rigidbody.velocity;
        Vector2 newVel;
        
        float stateElapsedFrames = currentTime - stateTimestamp;
        
        switch (movementState)
        {
        case MovementState.NORMAL:
            float xVelMax = Mathf.Abs(moveInput.x * movementSpeed);
            float xVelUncapped = oldVel.x + Mathf.Sign(moveInput.x) * movementAccel * deltaFrames;
            float xVel = Mathf.Max(-xVelMax, Mathf.Min(xVelMax, xVelUncapped));

            newVel = new Vector2(xVel, oldVel.y);
            break;
        case MovementState.DASHING:
            float dashDuration = dashDistance / dashSpeed;
            if (stateElapsedFrames > dashDuration)
            {
                // Finish dash
                _rigidbody.gravityScale = 1;
                if (grounded) hasDash = true;
                setMovementState(MovementState.NORMAL);
                return;
            }

            newVel = getDashVelocity(dashDirection, stateElapsedFrames);
            break;
        default:
            newVel = new Vector2(0, 0);
            break;
        }

        _rigidbody.velocity = newVel;
    }

    Vector2 getDashVelocity(float dashDirection, float stateElapsedFrames)
    {
        return new Vector2(dashDirection * dashSpeed, 0);
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnDash()
    {
        if (movementState == MovementState.NORMAL && hasDash)
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

    public void OnJump()
    {
        if (movementState == MovementState.NORMAL && grounded)
        {
            Vector2 oldVel = _rigidbody.velocity;

            float yVel = jumpSpeed;

            Vector2 newVel = new Vector2(oldVel.x, yVel);
            _rigidbody.velocity = newVel;
        }
        else
        {
            bufferInput(BufferedInput.JUMP);
        }
    }

    /// <summary>
    /// Sent when an incoming collider makes contact with this object's
    /// collider (2D physics only).
    /// </summary>
    /// <param name="other">The Collision2D data associated with this collision.</param>
    void OnCollisionEnter2D(Collision2D other)
    {
        grounded = true;
        hasDash = true;

        // Print how many points are colliding with this transform
        // Debug.Log("Points colliding: " + other.contacts.Length);

        // Print the normal of the first point in the collision.
        // Debug.Log("Normal of the first point: " + other.contacts[0].normal);

        // Draw a different colored ray for every normal in the collision
        foreach (var item in other.contacts)
        {
            Debug.DrawRay(item.point, item.normal * 100, Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f), 10f);
        }
    }

    /// <summary>
    /// Sent when a collider on another object stops touching this
    /// object's collider (2D physics only).
    /// </summary>
    /// <param name="other">The Collision2D data associated with this collision.</param>
    void OnCollisionExit2D(Collision2D other)
    {
        grounded = false;
    }

    void setMovementState(MovementState state) {
        movementState = state;
        stateTimestamp = currentTime;

        // Process buffered input if we returned to normal state
        if (state == MovementState.NORMAL &&
            currentTime - bufferedTimestamp <= inputBufferDuration)
        {
            // Debug.Log("Processing buffered input " + bufferedInput);
            switch (bufferedInput)
            {
            case BufferedInput.JUMP:
                OnJump();
                break;
            case BufferedInput.DASH:
                OnDash();
                break;
            }
        }
    }

    void bufferInput(BufferedInput input) {
        bufferedInput = input;
        bufferedTimestamp = currentTime;
        // Debug.Log("Buffered input " + input);
    }
}
