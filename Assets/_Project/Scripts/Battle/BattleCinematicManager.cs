using System.Collections;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;

namespace PixelMindscape.Battle
{
    public class BattleCinematicManager : MonoBehaviour
    {
        public static BattleCinematicManager Instance { get; private set; }

        [Header("All-Out Attack Settings")]
        [SerializeField] private GameObject allOutSplashPanel;
        [SerializeField] private Image allOutBackgroundImage;
        [SerializeField] private Image allOutCharacterArt; // The freeze frame art
        [SerializeField] private TMP_Text allOutText;
        [SerializeField] private float allOutDuration = 2.0f;

        [Header("Persona Summon Settings")]
        [SerializeField] private Material glitchMaterialPrefab;
        [SerializeField] private float summonFlashDuration = 0.5f;
        [SerializeField] private UnityEngine.Splines.SplineContainer personaSplineContainer;
        private GameObject activeSpawnedPersona;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            if (allOutSplashPanel != null) allOutSplashPanel.SetActive(false);

            if (personaSplineContainer == null)
            {
                GameObject splineObj = GameObject.Find("Spline_Persona_Animation");
                if (splineObj != null) personaSplineContainer = splineObj.GetComponent<UnityEngine.Splines.SplineContainer>();
            }
        }

        private void OnValidate()
        {
            if (personaSplineContainer == null)
            {
                GameObject splineObj = GameObject.Find("Spline_Persona_Animation");
                if (splineObj != null) personaSplineContainer = splineObj.GetComponent<UnityEngine.Splines.SplineContainer>();
            }
        }

        public IEnumerator PlayAllOutAttackSplashRoutine()
        {
            if (allOutSplashPanel == null) yield break;

            allOutSplashPanel.SetActive(true);
            
            // Basic freeze frame animation setup
            if (allOutBackgroundImage != null)
            {
                allOutBackgroundImage.color = Color.black;
                allOutBackgroundImage.DOColor(new Color(0.8f, 0, 0, 1), 0.5f).SetLoops(4, LoopType.Yoyo);
            }

            if (allOutText != null)
            {
                allOutText.transform.localScale = Vector3.zero;
                allOutText.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
            }

            // Wait for dramatic effect
            yield return new WaitForSeconds(allOutDuration);

            // Clean up
            allOutSplashPanel.SetActive(false);
        }

        public void PlayPersonaSummonFlash(Combatant combatant, PixelMindscape.Data.PersonaData personaData = null)
        {
            if (personaData != null && personaData.summonFlashFrames != null && personaData.summonFlashFrames.Length > 0)
            {
                if (activeSpawnedPersona != null) Destroy(activeSpawnedPersona);

                // Find or create a dedicated Cut-In Canvas!
                Canvas cutInCanvas = null;
                if (allOutSplashPanel != null) cutInCanvas = allOutSplashPanel.GetComponentInParent<Canvas>();
                if (cutInCanvas == null)
                {
                    var canvasObj = new GameObject("PersonaCutInCanvas");
                    cutInCanvas = canvasObj.AddComponent<Canvas>();
                    cutInCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    cutInCanvas.sortingOrder = 9999; // Render on top of EVERYTHING!
                    canvasObj.AddComponent<GraphicRaycaster>();
                }

                // Create the Cut-In UI Element
                activeSpawnedPersona = new GameObject($"UICutIn_{personaData.displayName}");
                activeSpawnedPersona.transform.SetParent(cutInCanvas.transform, false);

                var rect = activeSpawnedPersona.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(800, 600); // large, crisp anime cut-in size!

                var img = activeSpawnedPersona.AddComponent<Image>();
                img.sprite = personaData.summonFlashFrames[0];
                img.preserveAspect = true;

                // Set starting position off-screen left
                rect.anchoredPosition = new Vector2(-1500f, 0f);

                // DOTween Anime Sweep Choreography!
                Sequence cutInSeq = DOTween.Sequence();
                // Sweep in from left to center
                cutInSeq.Append(rect.DOAnchorPos(Vector2.zero, 0.3f).SetEase(Ease.OutBack));
                // Hover dramatically in center while coroutine plays frames
                cutInSeq.AppendInterval(0.4f);
                // Sweep out to the right
                cutInSeq.Append(rect.DOAnchorPos(new Vector2(1500f, 0f), 0.3f).SetEase(Ease.InCubic));
                cutInSeq.OnComplete(() => 
                {
                    if (activeSpawnedPersona != null) Destroy(activeSpawnedPersona);
                });

                Debug.Log($"[BattleCinematicManager] SUCCESS! Launched spectacular UI Canvas Cut-In for Persona '{personaData.displayName}'!");

                StartCoroutine(PlayUICutInFramesRoutine(img, personaData.summonFlashFrames));
            }
            else if (personaData != null && personaData.personaModelPrefab != null)
            {
                // Fallback to 3D model spawning at the combatant's position if they use a 3D model instead of sprites!
                if (activeSpawnedPersona != null) Destroy(activeSpawnedPersona);
                
                Vector3 spawnPos = combatant.transform.position + new Vector3(0, 1f, 0); // hover right above/behind combatant
                activeSpawnedPersona = Instantiate(personaData.personaModelPrefab, spawnPos, Quaternion.identity);
                activeSpawnedPersona.transform.localScale = Vector3.zero;
                activeSpawnedPersona.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);

                // Destroy after 1.5 seconds
                DOVirtual.DelayedCall(1.5f, () => { if (activeSpawnedPersona != null) Destroy(activeSpawnedPersona); });
                
                Debug.Log($"[BattleCinematicManager] SUCCESS! Spawned Persona Model '{personaData.displayName}' above combatant!");
            }

            // Always play the hero's white/cyan flash and glitch effect!
            var spriteRenderer = combatant.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer == null) return;

            var originalColor = spriteRenderer.color;
            
            Sequence summonSeq = DOTween.Sequence();
            summonSeq.Append(spriteRenderer.DOColor(Color.white, 0.1f));
            summonSeq.Append(spriteRenderer.DOColor(Color.cyan, 0.1f));
            summonSeq.Append(spriteRenderer.DOColor(Color.white, 0.1f));
            summonSeq.Append(spriteRenderer.DOColor(originalColor, 0.2f));

            if (glitchMaterialPrefab != null)
            {
                var originalMat = spriteRenderer.material;
                spriteRenderer.material = glitchMaterialPrefab;
                DOVirtual.DelayedCall(summonFlashDuration, () => 
                {
                    if (spriteRenderer != null) spriteRenderer.material = originalMat;
                });
            }
        }

        private IEnumerator PlayUICutInFramesRoutine(Image img, Sprite[] frames)
        {
            float totalAnimTime = 0.7f; // covers the sweep-in and hover duration!
            float frameDuration = totalAnimTime / frames.Length;

            for (int i = 0; i < frames.Length; i++)
            {
                if (img != null && frames[i] != null) img.sprite = frames[i];
                yield return new WaitForSeconds(frameDuration);
            }
        }
    }
}
