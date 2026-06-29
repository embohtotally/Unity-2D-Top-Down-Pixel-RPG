using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using PixelMindscape.Battle;
using PixelMindscape.Data;
using DG.Tweening;

namespace PixelMindscape.UI
{
    public class TargetSelectionView : MonoBehaviour
    {
        [Header("UI Prompt & Tooltip")]
        [SerializeField] private GameObject selectionPanel; // E.g., a panel that says "Select Target!"
        [SerializeField] private Button cancelButton; // Manual cancel button
        [SerializeField] private TextMeshProUGUI tooltipText; // Displays Name and Weaknesses

        [Header("Raycast Settings")]
        [SerializeField] private LayerMask targetLayerMask = ~0; // Layer mask for combatants

        [Header("Highlight Settings")]
        [SerializeField] private GameObject defaultHighlightPrefab;

        private List<Combatant> validTargets;
        private bool isSelecting = false;
        private Camera mainCamera;
        private int selectedIndex = 0;
        private float originalCamSize = 5f;
        private float originalCamFOV = 60f;
        private GameObject activeHighlightObj;
        
        private System.Action<Combatant> onTargetSelectedCallback;
        private System.Action onCancelledCallback;

        private void Start()
        {
            mainCamera = Camera.main;
            if (cancelButton != null) cancelButton.onClick.AddListener(CancelSelection);
            if (cancelButton != null) cancelButton.gameObject.SetActive(false);
            if (tooltipText != null) tooltipText.gameObject.SetActive(false);
        }

        public void Show(List<Combatant> targets, System.Action<Combatant> onSelected, System.Action onCancel = null)
        {
            validTargets = targets;
            onTargetSelectedCallback = onSelected;
            onCancelledCallback = onCancel;
            isSelecting = true;
            
            if (selectionPanel != null) selectionPanel.SetActive(true);
            if (cancelButton != null) cancelButton.gameObject.SetActive(true);
            if (tooltipText != null) tooltipText.gameObject.SetActive(true);

            // Camera Zoom
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera != null)
            {
                if (mainCamera.orthographic)
                {
                    originalCamSize = mainCamera.orthographicSize;
                    mainCamera.DOOrthoSize(originalCamSize * 0.9f, 0.3f).SetEase(Ease.OutCubic);
                }
                else
                {
                    originalCamFOV = mainCamera.fieldOfView;
                    mainCamera.DOFieldOfView(originalCamFOV * 0.9f, 0.3f).SetEase(Ease.OutCubic);
                }
            }

            if (validTargets != null && validTargets.Count > 0)
            {
                selectedIndex = 0;
                SelectTargetIndex(selectedIndex);
            }

            Debug.Log($"[TargetSelectionView] Ready for target selection (Controller & Raycast). Valid targets count: {(targets != null ? targets.Count : 0)}");
        }

        public void Hide()
        {
            isSelecting = false;
            validTargets = null;
            onTargetSelectedCallback = null;
            onCancelledCallback = null;
            if (selectionPanel != null) selectionPanel.SetActive(false);
            if (cancelButton != null) cancelButton.gameObject.SetActive(false);
            if (tooltipText != null) tooltipText.gameObject.SetActive(false);

            if (activeHighlightObj != null)
            {
                activeHighlightObj.transform.DOKill();
                activeHighlightObj.SetActive(false);
            }

            // Restore Camera Zoom
            if (mainCamera != null)
            {
                if (mainCamera.orthographic)
                {
                    mainCamera.DOOrthoSize(originalCamSize, 0.3f).SetEase(Ease.OutCubic);
                }
                else
                {
                    mainCamera.DOFieldOfView(originalCamFOV, 0.3f).SetEase(Ease.OutCubic);
                }
            }
        }

        private void SelectTargetIndex(int index)
        {
            if (validTargets == null || validTargets.Count == 0 || index < 0 || index >= validTargets.Count) return;

            var target = validTargets[index];

            // Setup TargetHighlight
            if (activeHighlightObj != null)
            {
                activeHighlightObj.transform.DOKill();
                activeHighlightObj.SetActive(false);
            }

            if (target.TargetHighlight == null)
            {
                // Dynamically create a spinning triangle / pulsating ring highlight if none assigned
                if (defaultHighlightPrefab != null)
                {
                    target.TargetHighlight = Instantiate(defaultHighlightPrefab, target.transform);
                }
                else
                {
                    // Create a beautiful default visual
                    GameObject ring = new GameObject("DynamicTargetHighlight");
                    ring.transform.SetParent(target.transform);
                    ring.transform.localPosition = Vector3.down * 0.2f;
                    var sr = ring.AddComponent<SpriteRenderer>();
                    sr.color = new Color(1f, 0.8f, 0.1f, 0.8f); // Gold / Yellow
                    // Create a simple placeholder square/circle sprite if available
                    sr.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                    ring.transform.localScale = new Vector3(1.5f, 0.5f, 1f);
                    target.TargetHighlight = ring;
                }
            }

            activeHighlightObj = target.TargetHighlight;
            if (activeHighlightObj != null)
            {
                activeHighlightObj.SetActive(true);
                activeHighlightObj.transform.DOKill();
                activeHighlightObj.transform.localScale = Vector3.one * 1.2f;
                activeHighlightObj.transform.DOScale(Vector3.one * 1.6f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
            }

            // Update Tooltip
            if (tooltipText != null)
            {
                string weakInfo = "None";
                if (target is EnemyCombatant enemy && enemy.EnemyData != null && enemy.EnemyData.affinityTable != null)
                {
                    List<string> weaknesses = new List<string>();
                    foreach (var entry in enemy.EnemyData.affinityTable)
                    {
                        if (entry.affinity == Affinity.Weak) weaknesses.Add(entry.element.ToString());
                    }
                    if (weaknesses.Count > 0) weakInfo = string.Join(", ", weaknesses);
                }
                tooltipText.text = $"{target.gameObject.name} | Weak: {weakInfo}";
            }
        }

        private void Update()
        {
            if (!isSelecting) return;

            // Controller Navigation (Left/Right or Up/Down)
            if (validTargets != null && validTargets.Count > 1)
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) || (Input.GetButtonDown("Horizontal") && Input.GetAxis("Horizontal") < 0))
                {
                    selectedIndex = (selectedIndex - 1 + validTargets.Count) % validTargets.Count;
                    SelectTargetIndex(selectedIndex);
                }
                else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) || (Input.GetButtonDown("Horizontal") && Input.GetAxis("Horizontal") > 0))
                {
                    selectedIndex = (selectedIndex + 1) % validTargets.Count;
                    SelectTargetIndex(selectedIndex);
                }
            }

            // Controller / Keyboard Submit
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Submit"))
            {
                if (validTargets != null && selectedIndex >= 0 && selectedIndex < validTargets.Count)
                {
                    var target = validTargets[selectedIndex];
                    var callback = onTargetSelectedCallback;
                    Hide();
                    callback?.Invoke(target);
                    return;
                }
            }

            // Handle Target Selection (Left Click)
            if (Input.GetMouseButtonDown(0))
            {
                HandleRaycastSelection();
            }

            // Handle Cancellation (Right Click or Escape / Cancel button)
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown("Cancel"))
            {
                CancelSelection();
            }
        }

        private void HandleRaycastSelection()
        {
            if (mainCamera == null) mainCamera = Camera.main;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            
            // 1. FIRST check Physics2D perspective ray intersection
            RaycastHit2D hit2D = Physics2D.GetRayIntersection(ray, Mathf.Infinity, targetLayerMask);
            Collider hitCollider = null;
            Collider2D hitCollider2D = hit2D.collider;

            // 2. SECOND check 3D Physics raycast
            if (hitCollider2D == null)
            {
                if (Physics.Raycast(ray, out RaycastHit hit3D, Mathf.Infinity, targetLayerMask))
                {
                    hitCollider = hit3D.collider;
                }
            }

            GameObject hitObj = hitCollider2D != null ? hitCollider2D.gameObject : (hitCollider != null ? hitCollider.gameObject : null);

            if (hitObj != null)
            {
                Debug.Log($"[TargetSelectionView] Raycast clicked. Hit Collider on: {hitObj.name}");

                Combatant target = hitObj.GetComponentInParent<Combatant>();
                if (target == null) target = hitObj.GetComponentInChildren<Combatant>();

                if (target != null)
                {
                    bool isValid = false;
                    if (validTargets != null && validTargets.Contains(target))
                    {
                        isValid = true;
                    }
                    else if (validTargets != null && validTargets.Count > 0)
                    {
                        bool seekingAlly = validTargets[0].IsPlayerSide;
                        if (target.IsPlayerSide == seekingAlly)
                        {
                            isValid = true;
                        }
                    }
                    else if (validTargets == null || validTargets.Count == 0)
                    {
                        isValid = true;
                    }

                    if (isValid)
                    {
                        Debug.Log($"[TargetSelectionView] Successfully selected valid target sprite: {target.gameObject.name}");
                        var callback = onTargetSelectedCallback;
                        Hide();
                        callback?.Invoke(target);
                    }
                    else
                    {
                        Debug.LogWarning($"[TargetSelectionView] Clicked on combatant '{target.gameObject.name}' (IsPlayerSide={target.IsPlayerSide}), but this action requires targets on the opposite side!");
                    }
                }
                else
                {
                    Debug.LogWarning($"[TargetSelectionView] Clicked on collider '{hitObj.name}', but no Combatant script was found on it or its parent/children.");
                }
            }
            else
            {
                Debug.Log($"[TargetSelectionView] Clicked at {Input.mousePosition}, but no 2D/3D Collider was hit.");
            }
        }

        private void CancelSelection()
        {
            var callback = onCancelledCallback;
            Hide();
            callback?.Invoke();
        }
    }
}
