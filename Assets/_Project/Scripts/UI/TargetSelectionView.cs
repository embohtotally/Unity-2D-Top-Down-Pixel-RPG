using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using PixelMindscape.Battle;

namespace PixelMindscape.UI
{
    public class TargetSelectionView : MonoBehaviour
    {
        [Header("UI Prompt")]
        [SerializeField] private GameObject selectionPanel; // E.g., a panel that says "Select Target!"
        [SerializeField] private Button cancelButton; // Manual cancel button

        [Header("Raycast Settings")]
        [SerializeField] private LayerMask targetLayerMask = ~0; // Layer mask for combatants

        private List<Combatant> validTargets;
        private bool isSelecting = false;
        private Camera mainCamera;
        
        private System.Action<Combatant> onTargetSelectedCallback;
        private System.Action onCancelledCallback;

        private void Start()
        {
            mainCamera = Camera.main;
            if (cancelButton != null) cancelButton.onClick.AddListener(CancelSelection);
        }

        public void Show(List<Combatant> targets, System.Action<Combatant> onSelected, System.Action onCancel = null)
        {
            validTargets = targets;
            onTargetSelectedCallback = onSelected;
            onCancelledCallback = onCancel;
            isSelecting = true;
            
            if (selectionPanel != null) selectionPanel.SetActive(true);

            Debug.Log($"[TargetSelectionView] Ready for sprite raycast selection. Valid targets count: {(targets != null ? targets.Count : 0)}");
        }

        public void Hide()
        {
            isSelecting = false;
            validTargets = null;
            onTargetSelectedCallback = null;
            onCancelledCallback = null;
            if (selectionPanel != null) selectionPanel.SetActive(false);
        }

        private void Update()
        {
            if (!isSelecting) return;

            // Handle Target Selection (Left Click)
            if (Input.GetMouseButtonDown(0))
            {
                HandleRaycastSelection();
            }

            // Handle Cancellation (Right Click)
            if (Input.GetMouseButtonDown(1))
            {
                CancelSelection();
            }
        }

        private void HandleRaycastSelection()
        {
            if (mainCamera == null) mainCamera = Camera.main;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            
            // 1. FIRST check Physics2D perspective ray intersection (flawless for Perspective & Orthographic cameras hitting 2D colliders)
            RaycastHit2D hit2D = Physics2D.GetRayIntersection(ray, Mathf.Infinity, targetLayerMask);
            Collider hitCollider = null;
            Collider2D hitCollider2D = hit2D.collider;

            // 2. SECOND check 3D Physics raycast (if the user added 3D BoxColliders in their perspective scene)
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
                            Debug.Log($"[TargetSelectionView] Target '{target.gameObject.name}' wasn't explicitly in validTargets list, but matches the required side (IsPlayerSide={target.IsPlayerSide}). Accepting!");
                            isValid = true;
                        }
                    }
                    else if (validTargets == null || validTargets.Count == 0)
                    {
                        Debug.Log($"[TargetSelectionView] validTargets list was empty, accepting clicked target '{target.gameObject.name}' as fallback.");
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
                Debug.Log($"[TargetSelectionView] Clicked at {Input.mousePosition}, but no 2D/3D Collider was hit. Ensure your character sprites have a BoxCollider2D/BoxCollider attached and match the TargetLayerMask.");
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
