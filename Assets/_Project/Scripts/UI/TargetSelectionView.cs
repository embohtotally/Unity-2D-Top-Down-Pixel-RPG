using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using PixelMindscape.Battle;

namespace PixelMindscape.UI
{
    public class TargetSelectionView : MonoBehaviour
    {
        [Header("UI (Optional)")]
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
            // 1. FIRST check Canvas UI Raycast (if enemies exist on or are tracked via the Canvas UI)
            if (EventSystem.current != null)
            {
                PointerEventData eventData = new PointerEventData(EventSystem.current)
                {
                    position = Input.mousePosition
                };
                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(eventData, results);

                foreach (RaycastResult result in results)
                {
                    Combatant uiTarget = result.gameObject.GetComponentInParent<Combatant>();
                    if (uiTarget == null) uiTarget = result.gameObject.GetComponentInChildren<Combatant>();

                    if (uiTarget != null && validTargets != null && validTargets.Contains(uiTarget) && !uiTarget.IsDefeated)
                    {
                        var callback = onTargetSelectedCallback;
                        Hide();
                        callback?.Invoke(uiTarget);
                        return; // Successfully selected via Canvas UI Raycast
                    }
                }
            }

            // 2. SECOND check Physics2D Raycast (if enemies are in the 2D World Space)
            if (mainCamera == null) mainCamera = Camera.main;

            Vector2 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            // Raycast at the mouse position to check for 2D colliders
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, Mathf.Infinity, targetLayerMask);

            if (hit.collider != null)
            {
                Combatant target = hit.collider.GetComponentInParent<Combatant>();
                
                if (target == null)
                    target = hit.collider.GetComponentInChildren<Combatant>();

                // Check if we hit a combatant, if it's in our valid list, and it's not dead.
                if (target != null && validTargets != null && validTargets.Contains(target) && !target.IsDefeated)
                {
                    var callback = onTargetSelectedCallback;
                    Hide();
                    callback?.Invoke(target);
                }
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
