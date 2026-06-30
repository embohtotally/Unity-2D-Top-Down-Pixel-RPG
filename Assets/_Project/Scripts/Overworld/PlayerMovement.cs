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

    private void Awake()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }



    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (scene.name.Contains("Battle"))
        {
            // We entered a Battle Scene! Freeze physics and let BattleManager take over.
            if (rb2D != null) rb2D.linearVelocity = Vector2.zero;
            this.enabled = false;
            Debug.Log($"[PlayerMovement] Entered Battle Scene '{scene.name}'. Disabling PlayerMovement.");
        }
        else
        {
            // ═══════════════════════════════════════════════════════
            // CRITICAL: Force-restore EVERY state that blocks movement
            // ═══════════════════════════════════════════════════════

            // FIX 1: Time.timeScale — NPCInteraction sets this to 0 when dialogue starts.
            // If the battle was triggered MID-DIALOGUE (via Fungus StartBattleCommand),
            // the NPC's EndDialogue() never ran, so timeScale is permanently stuck at 0.
            // FixedUpdate() simply does not execute when timeScale == 0.
            if (Time.timeScale < 0.01f)
            {
                Debug.LogWarning($"[PlayerMovement] Time.timeScale was {Time.timeScale} on Overworld return! Forcing to 1.0.");
                Time.timeScale = 1f;
            }

            // FIX 2: CutsceneDirector.IsCutscenePlaying — blocks FixedUpdate via early return
            if (PixelMindscape.Core.CutsceneDirector.Instance != null)
            {
                if (PixelMindscape.Core.CutsceneDirector.Instance.IsCutscenePlaying)
                {
                    Debug.LogWarning("[PlayerMovement] CutsceneDirector was still playing on Overworld return! Forcing EndCutscene.");
                }
                PixelMindscape.Core.CutsceneDirector.Instance.EndCutscene();
            }

            // FIX 3: Re-enable this script and clear all lock flags
            this.enabled = true;
            CutsceneMode = false;
            overrideMovement = Vector2.zero;

            // Move out of DontDestroyOnLoad back into the active Overworld scene hierarchy!
            if (gameObject.scene.name == "DontDestroyOnLoad")
            {
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gameObject, scene);

                // FIX 4: Restore pre-battle position so we don't spawn inside a wall
                if (PixelMindscape.Core.GameManager.Instance != null && PixelMindscape.Core.GameManager.Instance.HasSavedOverworldPosition)
                {
                    transform.position = PixelMindscape.Core.GameManager.Instance.LastOverworldPosition;
                    if (rb2D != null) rb2D.position = transform.position;
                    PixelMindscape.Core.GameManager.Instance.HasSavedOverworldPosition = false;
                    Debug.Log($"[PlayerMovement] Restored overworld position to {transform.position}.");
                }
            }

            Debug.Log($"[PlayerMovement] Returned to Overworld '{scene.name}'. enabled={this.enabled}, CutsceneMode={CutsceneMode}, timeScale={Time.timeScale}, rb2D={(rb2D != null ? "OK" : "NULL")}");
        }
    }

    public void SetOverrideMovement(Vector2 direction)
    {
        overrideMovement = direction;
    }

    public void SetCutsceneMode(bool state)
    {
        CutsceneMode = state;
    }

    private void Update()
    {
        // 1. Handle Input / Cutscenes
        if (CutsceneMode || (PixelMindscape.Core.CutsceneDirector.Instance != null && PixelMindscape.Core.CutsceneDirector.Instance.IsCutscenePlaying))
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
        if (CutsceneMode || (PixelMindscape.Core.CutsceneDirector.Instance != null && PixelMindscape.Core.CutsceneDirector.Instance.IsCutscenePlaying))
        {
            // Allow CutsceneDirector to move the transform directly via Lerp without Rigidbody fighting it!
            return;
        }

        if (rb2D != null)
        {
            // linearVelocity handles collisions perfectly without getting stuck like MovePosition
            rb2D.linearVelocity = movement * moveSpeed;

            // Optional: Hard clamp to map bounds if necessary (though invisible wall colliders are better)
            Vector2 pos = rb2D.position;
            bool clamped = false;
            
            if (pos.x < minBounds.x || pos.x > maxBounds.x) { pos.x = Mathf.Clamp(pos.x, minBounds.x, maxBounds.x); clamped = true; }
            if (pos.y < minBounds.y || pos.y > maxBounds.y) { pos.y = Mathf.Clamp(pos.y, minBounds.y, maxBounds.y); clamped = true; }
            
            if (clamped)
            {
                rb2D.position = pos;
            }
        }
    }
}
