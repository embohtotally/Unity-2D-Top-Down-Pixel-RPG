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
                // Instantiate and show pulsing highlights for ALL valid targets simultaneously!
                foreach (var target in validTargets)
                {
                    if (target.TargetHighlight == null)
                    {
                        CreateHighlightForTarget(target);
                    }
                    if (target.TargetHighlight != null)
                    {
                        target.TargetHighlight.SetActive(true);
                        target.TargetHighlight.transform.DOKill();
                        target.TargetHighlight.transform.localScale = Vector3.one * 1.2f;
                        target.TargetHighlight.transform.DOScale(Vector3.one * 1.6f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
                        
                        var fillImg = target.TargetHighlight.transform.Find("Fill")?.GetComponent<Image>();
                        if (fillImg != null)
                        {
                            fillImg.color = new Color(1f, 0.8f, 0.1f, 0.8f); // Bright Gold
                            fillImg.fillAmount = target.MaxHP > 0 ? (float)target.CurrentHP / target.MaxHP : 1f;
                        }
                    }
                }

                selectedIndex = 0;
                SelectTargetIndex(selectedIndex);
            }

            Debug.Log($"[TargetSelectionView] Ready for target selection (Controller & Raycast). Valid targets count: {(targets != null ? targets.Count : 0)}");
        }

        private static Sprite cachedRingSprite;
        private Sprite GetRingSprite()
        {
            if (cachedRingSprite != null) return cachedRingSprite;
            int radius = 64;
            int thickness = 12;
            Texture2D texture = new Texture2D(radius * 2, radius * 2);
            Color[] colors = new Color[texture.width * texture.height];
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius));
                    if (distance <= radius && distance >= radius - thickness)
                        colors[y * texture.width + x] = Color.white;
                    else
                        colors[y * texture.width + x] = Color.clear;
                }
            }
            texture.SetPixels(colors);
            texture.Apply();
            cachedRingSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            return cachedRingSprite;
        }

        private void CreateHighlightForTarget(Combatant target)
        {
            if (defaultHighlightPrefab != null)
            {
                target.TargetHighlight = Instantiate(defaultHighlightPrefab, target.transform);
            }
            else
            {
                GameObject canvasObj = new GameObject("DynamicTargetCanvas");
                canvasObj.transform.SetParent(target.transform);
                canvasObj.transform.localPosition = Vector3.down * 0.2f;
                var canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.sortingLayerName = "UI"; // Or whatever is above sprites
                canvas.sortingOrder = 50;
                canvasObj.AddComponent<CanvasScaler>();
                
                var rt = canvasObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(1.5f, 0.5f); // Flatten it for perspective

                // Background Ring (Dim)
                GameObject bgObj = new GameObject("Background");
                bgObj.transform.SetParent(canvasObj.transform, false);
                var bgImg = bgObj.AddComponent<Image>();
                bgImg.sprite = GetRingSprite();
                bgImg.color = new Color(0, 0, 0, 0.5f);
                var bgRT = bgObj.GetComponent<RectTransform>();
                bgRT.anchorMin = Vector2.zero;
                bgRT.anchorMax = Vector2.one;
                bgRT.sizeDelta = Vector2.zero;

                // Fill Ring (Gold)
                GameObject fillObj = new GameObject("Fill");
                fillObj.transform.SetParent(canvasObj.transform, false);
                var fillImg = fillObj.AddComponent<Image>();
                fillImg.sprite = GetRingSprite();
                fillImg.type = Image.Type.Filled;
                fillImg.fillMethod = Image.FillMethod.Radial360;
                fillImg.fillOrigin = 2; // Top
                fillImg.fillClockwise = true;
                fillImg.color = new Color(1f, 0.8f, 0.1f, 0.8f);
                var fillRT = fillObj.GetComponent<RectTransform>();
                fillRT.anchorMin = Vector2.zero;
                fillRT.anchorMax = Vector2.one;
                fillRT.sizeDelta = Vector2.zero;

                target.TargetHighlight = canvasObj;
            }
        }

        public void Hide()
        {
            isSelecting = false;
            
            if (selectionPanel != null) selectionPanel.SetActive(false);
            if (cancelButton != null) cancelButton.gameObject.SetActive(false);
            if (tooltipText != null) tooltipText.gameObject.SetActive(false);

            if (validTargets != null)
            {
                foreach (var target in validTargets)
                {
                    if (target != null && target.TargetHighlight != null)
                    {
                        target.TargetHighlight.transform.DOKill();
                        target.TargetHighlight.SetActive(false);
                    }
                }
            }
            
            validTargets = null;
            onTargetSelectedCallback = null;
            onCancelledCallback = null;

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

            if (target.TargetHighlight == null)
            {
                CreateHighlightForTarget(target);
                target.TargetHighlight.SetActive(true);
                target.TargetHighlight.transform.DOKill();
                target.TargetHighlight.transform.localScale = Vector3.one * 1.2f;
                target.TargetHighlight.transform.DOScale(Vector3.one * 1.6f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
                var fillImg = target.TargetHighlight.transform.Find("Fill")?.GetComponent<Image>();
                if (fillImg != null)
                {
                    fillImg.color = new Color(1f, 0.8f, 0.1f, 0.8f);
                    fillImg.fillAmount = target.MaxHP > 0 ? (float)target.CurrentHP / target.MaxHP : 1f;
                }
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

                // Generate JRPG ASCII Health Bar
                int hpPercent = target.MaxHP > 0 ? Mathf.RoundToInt(((float)target.CurrentHP / target.MaxHP) * 100f) : 0;
                int totalBlocks = 10;
                int filledBlocks = target.MaxHP > 0 ? Mathf.RoundToInt(((float)target.CurrentHP / target.MaxHP) * totalBlocks) : 0;
                string bar = new string('█', filledBlocks) + new string('░', totalBlocks - filledBlocks);

                tooltipText.text = $"<b>{target.gameObject.name}</b>\n<color=#FF5555>HP: [{bar}] {hpPercent}%</color>\nWeak: {weakInfo}";

                // Optionally, if tooltipText is in a Screen Space overlay, we can snap its position to the target!
                if (mainCamera == null) mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    var rt = tooltipText.rectTransform;
                    if (rt.parent != null && rt.parent.GetComponent<Canvas>() != null && rt.parent.GetComponent<Canvas>().renderMode != RenderMode.WorldSpace)
                    {
                        // Snap the tooltip rect to the enemy's screen position
                        Vector3 screenPos = mainCamera.WorldToScreenPoint(target.transform.position + Vector3.up * 1.5f);
                        rt.position = screenPos;
                    }
                }
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
