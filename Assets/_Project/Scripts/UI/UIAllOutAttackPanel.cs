using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PixelMindscape.Battle;
using DG.Tweening;

namespace PixelMindscape.UI
{
    public class UIAllOutAttackPanel : MonoBehaviour
    {
        [SerializeField] private GameObject overlayPanel;
        [SerializeField] private RectTransform portraitsContainer; // Holds the 4 character portraits arranged in diamond/cross
        [SerializeField] private TextMeshProUGUI pressAttackPrompt;
        [SerializeField] private TextMeshProUGUI totalDamageText;
        [SerializeField] private GameObject explosionVFXPrefab;

        private bool isWaitingForInput = false;

        private void Start()
        {
            if (overlayPanel != null) overlayPanel.SetActive(false);
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.OnPromptAllOutAttack += ShowPrompt;
            }
        }

        private void OnDestroy()
        {
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.OnPromptAllOutAttack -= ShowPrompt;
            }
        }

        public void ShowPrompt()
        {
            if (overlayPanel == null) return;

            overlayPanel.SetActive(true);
            isWaitingForInput = true;

            if (totalDamageText != null) totalDamageText.gameObject.SetActive(false);

            if (pressAttackPrompt != null)
            {
                pressAttackPrompt.gameObject.SetActive(true);
                pressAttackPrompt.transform.DOKill();
                pressAttackPrompt.transform.localScale = Vector3.one;
                pressAttackPrompt.transform.DOScale(1.2f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
            }

            if (portraitsContainer != null)
            {
                portraitsContainer.localScale = Vector3.zero;
                portraitsContainer.localRotation = Quaternion.Euler(0, 0, 180);
                portraitsContainer.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
                portraitsContainer.DORotate(Vector3.zero, 0.5f).SetEase(Ease.OutBack);
            }
        }

        private void Update()
        {
            if (!isWaitingForInput) return;

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetButtonDown("Submit") || Input.GetMouseButtonDown(0))
            {
                isWaitingForInput = false;
                StartCoroutine(ExecuteAllOutAttackRoutine());
            }
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetButtonDown("Cancel") || Input.GetMouseButtonDown(1))
            {
                isWaitingForInput = false;
                if (overlayPanel != null) overlayPanel.SetActive(false);
                if (BattleManager.Instance != null) BattleManager.Instance.CancelAllOutPrompt();
            }
        }

        private IEnumerator ExecuteAllOutAttackRoutine()
        {
            if (pressAttackPrompt != null) pressAttackPrompt.gameObject.SetActive(false);

            if (portraitsContainer != null)
            {
                portraitsContainer.DOScale(Vector3.one * 1.5f, 0.3f);
                // Cannot call DOFade directly on RectTransform without CanvasGroup, so let's scale to zero for clean exit
                portraitsContainer.DOScale(Vector3.zero, 0.3f).SetDelay(0.2f);
            }

            if (explosionVFXPrefab != null)
            {
                Instantiate(explosionVFXPrefab, overlayPanel.transform);
            }

            yield return new WaitForSeconds(0.5f);

            if (totalDamageText != null)
            {
                totalDamageText.gameObject.SetActive(true);
                totalDamageText.text = "9999 DAMAGE!";
                totalDamageText.transform.localScale = Vector3.zero;
                totalDamageText.transform.DOScale(Vector3.one * 2f, 0.5f).SetEase(Ease.OutBounce);
            }

            yield return new WaitForSeconds(1.5f);

            if (overlayPanel != null) overlayPanel.SetActive(false);
            if (BattleManager.Instance != null) BattleManager.Instance.ResolveAllOutAttack();
        }
    }
}
