using UnityEngine;
using System.Timers;
using System;
using System.Collections;
using System.Diagnostics;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    private Camera mainCamera;
    private Rigidbody2D rb;
    private Collider2D capsuleCollider;

    private Vector2 velocity;
    private float inputAxis;

    public float moveSpeed = 8f;
    public float maxJumpHeight = 5f;
    public float maxJumpTime = 1f;
    public float jumpForce => (2f * maxJumpHeight) / (maxJumpTime / 2f);
    public float gravity => (-2f * maxJumpHeight) / Mathf.Pow(maxJumpTime / 2f, 2f);

    public bool grounded { get; private set; }
    public bool jumping { get; private set; }
    public bool running => Mathf.Abs(velocity.x) > 0.25f || Mathf.Abs(inputAxis) > 0.25f;
    public bool sliding => (inputAxis > 0f && velocity.x < 0f) || (inputAxis < 0f && velocity.x > 0f);
    public bool falling => velocity.y < 0f && !grounded;

    private static Timer timer;
    private static bool timerFinished = false;

    private void Awake()
    {
        mainCamera = Camera.main;
        rb = GetComponent<Rigidbody2D>();
        capsuleCollider = GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        rb.isKinematic = false;
        capsuleCollider.enabled = true;
        velocity = Vector2.zero;
        jumping = false;
    }

    private void OnDisable()
    {
        rb.isKinematic = true;
        capsuleCollider.enabled = false;
        velocity = Vector2.zero;
        inputAxis = 0f;
        jumping = false;
    }
    
    // private void Update()
    // {
    //     HorizontalMovement();

    //     grounded = rb.Raycast(Vector2.down);

    //     ApplyGravity();
    // }


    private void FixedUpdate()
    {
        // Move mario based on his velocity
        Vector2 position = rb.position;
        position += velocity * Time.fixedDeltaTime;

        // Clamp within the screen bounds
        Vector2 leftEdge = mainCamera.ScreenToWorldPoint(Vector2.zero);
        Vector2 rightEdge = mainCamera.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));
        position.x = Mathf.Clamp(position.x, leftEdge.x + 0.5f, rightEdge.x - 0.5f);

        rb.MovePosition(position);
    }

    public void HorizontalMovement()
    {
        // Accelerate / decelerate
        // inputAxis = Input.GetAxis("Horizontal");
        inputAxis = 1f;
        velocity.x = Mathf.MoveTowards(velocity.x, inputAxis * moveSpeed, moveSpeed * Time.deltaTime);

        // Check if running into a wall
        if (rb.Raycast(Vector2.right * velocity.x)) {
            velocity.x = 0f;
        }

        // Flip sprite to face direction
        if (velocity.x > 0f) {
            transform.eulerAngles = Vector3.zero;
        } else if (velocity.x < 0f) {
            transform.eulerAngles = new Vector3(0f, 180f, 0f);
        }
    }


    

    public IEnumerator GroundedMovement(float duration)
    {
        // Prevent gravity from infinitly building up
        velocity.y = Mathf.Max(velocity.y, 0f);
        jumping = velocity.y > 0f;

        Stopwatch s = new Stopwatch();
        s.Start();
            

        
        // While loop that continues until timer finishes
        while (s.Elapsed < TimeSpan.FromMilliseconds(duration)) 
        {
            velocity.y = jumpForce;
            jumping = true;
            yield return null;
        }
        
        
     
    }
    


    public void ApplyGravity()
    {
        // Check if falling
        bool falling = velocity.y < 0f || !Input.GetButton("Jump");
        float multiplier = falling ? 2f : 1f;

        // Apply gravity and terminal velocity
        velocity.y += gravity * multiplier * Time.deltaTime;
        velocity.y = Mathf.Max(velocity.y, gravity / 2f);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            // Bounce off enemy head
            if (transform.DotTest(collision.transform, Vector2.down))
            {
                velocity.y = jumpForce / 2f;
                jumping = true;
            }
        }
        else if (collision.gameObject.layer != LayerMask.NameToLayer("PowerUp"))
        {
            // Stop vertical movement if mario bonks his head
            if (transform.DotTest(collision.transform, Vector2.up)) {
                velocity.y = 0f;
            }
        }
    }

}
