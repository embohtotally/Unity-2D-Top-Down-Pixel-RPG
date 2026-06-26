using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    Rigidbody2D rb2D;
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] Vector2 minBounds = new Vector2(-111f, -111f);
    [SerializeField] Vector2 maxBounds = new Vector2(111f, 111f);

    Vector2 movement;
    Animator animator;
    SpriteRenderer spriteRenderer;

    public bool CutsceneMode { get; set; }
    private Vector2 overrideMovement;
    private float lastHorizontal = 1f; 

    // Cached Animator Hashes for performance
    private int horizontalHash;
    private int verticalHash;
    private int speedHash;

    private void Start()
    {
        animator = GetComponent<Animator>();
        rb2D = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>(); 

        if (rb2D != null && rb2D.gravityScale != 0)
        {
            rb2D.gravityScale = 0f;
        }

        // Cache hashes once at startup
        horizontalHash = Animator.StringToHash("Horizontal");
        verticalHash = Animator.StringToHash("Vertical");
        speedHash = Animator.StringToHash("Speed");

        // Set static animation parameters once
        animator.SetFloat(verticalHash, 0f);
    }

    public void SetOverrideMovement(Vector2 direction)
    {
        overrideMovement = direction;
    }

    private void Update()
    {
        // 1. Handle Input / Cutscenes
        if (CutsceneMode)
        {
            movement = overrideMovement;
        }
        else
        {
            movement.x = Input.GetAxisRaw("Horizontal");
            movement.y = Input.GetAxisRaw("Vertical");
        }

        // Clamp magnitude to prevent 41% faster diagonal movement
        movement = Vector2.ClampMagnitude(movement, 1f);

        // 2. Update memory ONLY when a horizontal key is actively held down
        if (movement.x != 0)
        {
            lastHorizontal = movement.x;
        }

        // 3. Handle Sprite Flipping based on memory
        if (lastHorizontal < 0)
        {
            spriteRenderer.flipX = false; // Face Left
        }
        else if (lastHorizontal > 0)
        {
            spriteRenderer.flipX = true;  // Face Right
        }

        // 4. Update Animator using cached hashes
        animator.SetFloat(horizontalHash, lastHorizontal);
        animator.SetFloat(speedHash, movement.sqrMagnitude);
    }

    void FixedUpdate()
    {
        // Calculate new physics position
        Vector2 targetPosition = rb2D.position + movement * moveSpeed * Time.fixedDeltaTime;
        
        // Clamp position safely within the physics engine
        targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
        targetPosition.y = Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y);

        rb2D.MovePosition(targetPosition);
    }
}
