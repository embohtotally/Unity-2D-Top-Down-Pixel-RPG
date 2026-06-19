using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using PixelMindscape.Core;
using DG.Tweening;

[System.Serializable]
public struct DialogueLine
{
    public string speakerName;
    [TextArea(3, 5)]
    public string dialogueText;
    public Sprite speakerImageA;
    public Sprite speakerImageB;
    public bool isSpeakerAActive;
}

public class NPCInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRadius = 2f;
    [SerializeField] private GameObject interactIndicator;

    [Header("Dialogue Settings")]
    [SerializeField] private List<DialogueLine> dialogueLines;
    [SerializeField] private GameObject dialogueUI;
    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Image speakerImageA;
    [SerializeField] private Image speakerImageB;
    [SerializeField] private GameObject skipButton;

    [Header("Skip Dialogue")]
    [SerializeField] private GameObject skipPanel;
    [SerializeField] private TextMeshProUGUI skipSummaryText;
    [SerializeField][TextArea(2, 5)] private string skipSummary;

    [Header("Typing Settings")]
    [SerializeField] private float typingSpeed = 0.03f;

    [Header("Patrol Settings")]
    [SerializeField] private Transform patrolPoint;
    [SerializeField] private float patrolSpeed = 2f;

    [Header("Auto Dialogue Trigger")]
    [SerializeField] private bool autoStartDialogue = false;
    [SerializeField] private bool oneTimeOnly = false;
    [SerializeField] private float autoStartDelay = 3.0f;

    [SerializeField] private GameObject blurPanel;

    [Header("Animation Settings")]
    [SerializeField] private RectTransform dialogueBoxRect;
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private Vector2 dialogueBoxStartOffset = new Vector2(0, -200f);
    [SerializeField] private Vector2 speakerAStartOffset = new Vector2(-200f, 0);
    [SerializeField] private Vector2 speakerBStartOffset = new Vector2(200f, 0);

    [Header("Events")]
    public UnityEngine.Events.UnityEvent onDialogueFinished;

    [Header("Reference")]
    [SerializeField] GameManager gameManager;

    private bool isPlayerInRange = false;
    private bool isPatrolling = false;
    private bool isTyping = false;
    private bool isSkipPanelOpen = false;
    private bool isDialogueActive = false;
    private bool hasTriggered = false;

    private string currentFullLine = "";
    private Coroutine typingCoroutine;
    private Coroutine autoStartCoroutine;
    private Queue<DialogueLine> dialogueQueue;

    private float previousTimeScale = 1f;

    // --- FIX START: Variables to store your Inspector Scale ---
    private Vector3 defaultScaleA;
    private Vector3 defaultScaleB;
    // --- FIX END ---
    
    private Vector2 originalDialogueBoxPos;
    private Vector2 originalSpeakerAPos;
    private Vector2 originalSpeakerBPos;
    private bool isSpeakerAVisible = false;
    private bool isSpeakerBVisible = false;

    private void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();
    }

    private void Start()
    {
        dialogueQueue = new Queue<DialogueLine>();
        dialogueUI.SetActive(false);

        if (interactIndicator != null)
            interactIndicator.SetActive(false);

        // --- FIX START: Remember the scale you set in Inspector (e.g., 5) ---
        if (speakerImageA != null) defaultScaleA = speakerImageA.transform.localScale;
        if (speakerImageB != null) defaultScaleB = speakerImageB.transform.localScale;
        // --- FIX END ---

        if (dialogueBoxRect != null) originalDialogueBoxPos = dialogueBoxRect.anchoredPosition;
        if (speakerImageA != null) originalSpeakerAPos = speakerImageA.rectTransform.anchoredPosition;
        if (speakerImageB != null) originalSpeakerBPos = speakerImageB.rectTransform.anchoredPosition;

        speakerImageA.enabled = false;
        speakerImageB.enabled = false;

        if (autoStartDialogue && !hasTriggered)
        {
            autoStartCoroutine = StartCoroutine(WaitAndStartDialogue());
        }
    }

    private IEnumerator WaitAndStartDialogue()
    {
        yield return new WaitForSeconds(autoStartDelay);

        if (!isDialogueActive && (!oneTimeOnly || !hasTriggered))
        {
            StartDialogue();
        }

        autoStartCoroutine = null;
    }

    public void OnInteractButtonClicked()
    {
        if (!isDialogueActive && isPlayerInRange)
        {
            StartDialogue();
        }
    }

    private void Update()
    {
        if (isSkipPanelOpen) return;

        // Advance Dialogue
        if (isDialogueActive)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.F))
            {
                if (isTyping)
                {
                    if (typingCoroutine != null) StopCoroutine(typingCoroutine);
                    dialogueText.text = currentFullLine;
                    isTyping = false;
                    typingCoroutine = null;
                }
                else
                {
                    DisplayNextSentence();
                }
            }
            return;
        }

        // Manual Start
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.F))
        {
            StartDialogue();
        }

        // Patrol
        if (isPatrolling)
        {
            float step = patrolSpeed * Time.unscaledDeltaTime;
            transform.position = Vector3.MoveTowards(transform.position, patrolPoint.position, step);

            if (Vector3.Distance(transform.position, patrolPoint.position) < 0.01f)
            {
                isPatrolling = false;
            }
        }
    }

    public void StartDialogue()
    {
        if (autoStartCoroutine != null)
        {
            StopCoroutine(autoStartCoroutine);
            autoStartCoroutine = null;
        }

        if (hasTriggered && oneTimeOnly)
        {
            return;
        }

        if (dialogueLines.Count == 0)
        {
            return;
        }

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        isDialogueActive = true;
        dialogueQueue.Clear();

        foreach (var line in dialogueLines)
            dialogueQueue.Enqueue(line);

        dialogueUI.SetActive(true);
        if (interactIndicator != null) interactIndicator.SetActive(false);
        if (blurPanel != null) blurPanel.SetActive(true);
        if (skipButton != null) skipButton.SetActive(true);

        isSpeakerAVisible = false;
        isSpeakerBVisible = false;

        if (dialogueBoxRect != null)
        {
            dialogueBoxRect.anchoredPosition = originalDialogueBoxPos + dialogueBoxStartOffset;
            dialogueBoxRect.DOAnchorPos(originalDialogueBoxPos, animationDuration).SetUpdate(true).SetEase(Ease.OutBack);
        }

        DisplayNextSentence();
    }

    private void DisplayNextSentence()
    {
        if (dialogueQueue.Count == 0)
        {
            EndDialogue();
            return;
        }

        var currentLine = dialogueQueue.Dequeue();

        speakerText.text = currentLine.speakerName;
        currentFullLine = currentLine.dialogueText;

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeSentence(currentFullLine));

        // --- FIX START: Use the stored defaultScale instead of Vector3.one ---

        Color bright = Color.white;
        Color dim = new Color(0.8f, 0.8f, 0.8f, 1f);

        if (currentLine.speakerImageA != null)
        {
            speakerImageA.enabled = true;
            speakerImageA.sprite = currentLine.speakerImageA;

            if (!isSpeakerAVisible)
            {
                speakerImageA.rectTransform.anchoredPosition = originalSpeakerAPos + speakerAStartOffset;
                speakerImageA.rectTransform.DOAnchorPos(originalSpeakerAPos, animationDuration).SetUpdate(true).SetEase(Ease.OutBack);
                isSpeakerAVisible = true;
            }

            // Calculate dimmed scale based on YOUR default scale (e.g. 5 becomes 4.5)
            Vector3 dimmedScaleA = defaultScaleA * 0.9f;

            speakerImageA.transform.localScale = currentLine.isSpeakerAActive ? defaultScaleA : dimmedScaleA;
            speakerImageA.color = currentLine.isSpeakerAActive ? bright : dim;
        }
        else
        {
            speakerImageA.enabled = false;
            isSpeakerAVisible = false;
        }

        if (currentLine.speakerImageB != null)
        {
            speakerImageB.enabled = true;
            speakerImageB.sprite = currentLine.speakerImageB;

            if (!isSpeakerBVisible)
            {
                speakerImageB.rectTransform.anchoredPosition = originalSpeakerBPos + speakerBStartOffset;
                speakerImageB.rectTransform.DOAnchorPos(originalSpeakerBPos, animationDuration).SetUpdate(true).SetEase(Ease.OutBack);
                isSpeakerBVisible = true;
            }

            // Calculate dimmed scale based on YOUR default scale
            Vector3 dimmedScaleB = defaultScaleB * 0.9f;

            speakerImageB.transform.localScale = currentLine.isSpeakerAActive ? dimmedScaleB : defaultScaleB;
            speakerImageB.color = currentLine.isSpeakerAActive ? dim : bright;
        }
        else
        {
            speakerImageB.enabled = false;
            isSpeakerBVisible = false;
        }
        // --- FIX END ---
    }

    private void EndDialogue()
    {
        Time.timeScale = previousTimeScale;

        isDialogueActive = false;
        dialogueUI.SetActive(false);
        if (skipButton != null) skipButton.SetActive(false);
        if (blurPanel != null) blurPanel.SetActive(false);

        speakerText.text = "";
        dialogueText.text = "";

        speakerImageA.enabled = false;
        speakerImageB.enabled = false;

        hasTriggered = oneTimeOnly || hasTriggered;

        if (interactIndicator != null && isPlayerInRange)
        {
            if (oneTimeOnly && hasTriggered && patrolPoint != null)
            {
                interactIndicator.SetActive(false);
            }
            else if (!(oneTimeOnly && hasTriggered))
            {
                interactIndicator.SetActive(true);
            }
        }

        if (oneTimeOnly && patrolPoint != null)
        {
            isPatrolling = true;
        }

        onDialogueFinished?.Invoke();
        // gameManager.GameStart(); // Deprecated in PixelMindscape.Core
    }

    public void OnSkipButtonPressed()
    {
        if (!isDialogueActive) return;

        if (skipPanel != null)
        {
            skipPanel.SetActive(true);
            skipSummaryText.text = skipSummary;
            isSkipPanelOpen = true;

            if (skipButton != null) skipButton.SetActive(false);
        }
    }

    public void ConfirmSkip()
    {
        isSkipPanelOpen = false;

        while (dialogueQueue.Count > 0)
            dialogueQueue.Dequeue();

        EndDialogue();

        if (skipPanel != null) skipPanel.SetActive(false);
    }

    public void CancelSkip()
    {
        isSkipPanelOpen = false;

        if (skipPanel != null) skipPanel.SetActive(false);
        if (skipButton != null) skipButton.SetActive(true);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerInRange = true;

        if (interactIndicator != null)
            interactIndicator.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;

            if (interactIndicator != null)
                interactIndicator.SetActive(false);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }

    private IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isTyping = false;
        typingCoroutine = null;
    }
}