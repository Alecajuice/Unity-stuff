using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public Transform movePoint;
    public Rigidbody2D rb;
    public LayerMask whatStopsMovement;
    public Animator animator;

    Vector2 moveInput;

    // Start is called before the first frame update
    void Start()
    {
        movePoint.parent = null;
    }

    // Update is called once per frame
    void Update()
    {
    }

    /// <summary>
    /// This function is called every fixed framerate frame, if the MonoBehaviour is enabled.
    /// </summary>
    void FixedUpdate()
    {
        // Move towards movePoint
        Vector3 newPosition = Vector3.MoveTowards(transform.position, movePoint.position, moveSpeed * Time.fixedDeltaTime);
        Vector3 movement = newPosition - transform.position;
        transform.position = newPosition;

        // Update animator
        animator.SetFloat("Horizontal", movement.x);
        animator.SetFloat("Vertical", movement.y);
        animator.SetBool("Moving", movement.sqrMagnitude > 0f);
        // Set horizontal scale to mirror when going left
        Vector3 localScale = transform.localScale;
        localScale.x = movement.x >= 0 ? 1 : -1;
        transform.localScale = localScale;

        // Calculate new movePoint
        Vector3 newMovePoint = movePoint.position;
        if (Vector3.Distance(transform.position, movePoint.position) <= 0.05f) { // Only move if player close to movePoint
            if (Mathf.Abs(moveInput.x) == 1f) {
                newMovePoint += new Vector3(Mathf.Sign(moveInput.x), 0, 0);
            } else if (Mathf.Abs(moveInput.y) == 1f) {
                newMovePoint += new Vector3(0, Mathf.Sign(moveInput.y), 0);
            }
        }

        // Apply movePoint change if no colliders in the way
        if (!Physics2D.OverlapCircle(newMovePoint, 0.2f, whatStopsMovement)) {
            movePoint.position = newMovePoint;
        }
    }

    /* Input */

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
}
