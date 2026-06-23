using System.Collections;
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

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            if (allOutSplashPanel != null) allOutSplashPanel.SetActive(false);
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

        public void PlayPersonaSummonFlash(Combatant combatant)
        {
            var spriteRenderer = combatant.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer == null) return;

            // 6-10 frame flash sequence
            // Turn them bright white/cyan then return to normal using DOTween
            var originalColor = spriteRenderer.color;
            
            Sequence summonSeq = DOTween.Sequence();
            summonSeq.Append(spriteRenderer.DOColor(Color.white, 0.1f));
            summonSeq.Append(spriteRenderer.DOColor(Color.cyan, 0.1f));
            summonSeq.Append(spriteRenderer.DOColor(Color.white, 0.1f));
            summonSeq.Append(spriteRenderer.DOColor(originalColor, 0.2f));

            // Optional: apply a glitch material briefly
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
    }
}
