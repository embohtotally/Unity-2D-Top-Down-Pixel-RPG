using UnityEngine;

namespace PixelMindscape.Battle
{
    public class CombatantVFXHandler : MonoBehaviour
    {
        [Header("Attachment Point")]
        [Tooltip("The transform where visual effects should be spawned or played. Defaults to this transform if unassigned.")]
        [SerializeField] private Transform vfxSpawnPoint;

        [Header("Common VFX Prefabs")]
        [SerializeField] private GameObject defaultHitVFX;
        [SerializeField] private GameObject defaultAttackVFX;
        [SerializeField] private GameObject guardVFX;
        [SerializeField] private GameObject batonPassVFX;

        [Header("Settings")]
        [SerializeField] private float autoDestroyDelay = 2.0f;

        private void Awake()
        {
            if (vfxSpawnPoint == null)
            {
                vfxSpawnPoint = transform;
            }
        }

        /// Instantiates and plays a specific VFX prefab at the spawn point
        public void PlayVFX(GameObject vfxPrefab)
        {
            if (vfxPrefab == null) return;

            GameObject spawnedVFX = Instantiate(vfxPrefab, vfxSpawnPoint.position, vfxSpawnPoint.rotation, vfxSpawnPoint);
            
            // If the prefab doesn't have its own auto-destroy script, clean it up after a delay
            Destroy(spawnedVFX, autoDestroyDelay);
        }

        public void PlayHitVFX() => PlayVFX(defaultHitVFX);
        public void PlayAttackVFX() => PlayVFX(defaultAttackVFX);
        public void PlayGuardVFX() => PlayVFX(guardVFX);
        public void PlayBatonPassVFX() => PlayVFX(batonPassVFX);
    }
}
