using UnityEngine;
using System.Collections.Generic;
using PixelMindscape.Core;

namespace PixelMindscape.Overworld
{
    [RequireComponent(typeof(Collider2D))]
    public class BattleTrigger : MonoBehaviour
    {
        [Header("Battle Settings")]
        [Tooltip("The name of the Battle scene to load")]
        public string battleSceneName = "Battle_Standard";

        [Tooltip("The enemies that will spawn in this battle")]
        public List<GameObject> enemyPrefabs = new List<GameObject>();

        [Header("Trigger Settings")]
        [Tooltip("If true, the battle starts automatically when the player touches this object.")]
        public bool triggerOnCollision = true;
        
        [Tooltip("Destroy this object after the battle is triggered?")]
        public bool destroyAfterTrigger = true;

        private bool hasTriggered = false;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (hasTriggered || !triggerOnCollision) return;

            if (collision.CompareTag("Player"))
            {
                StartBattle();
            }
        }

        /// <summary>
        /// Call this method from a UI Button or a custom C# script to start the battle manually!
        /// </summary>
        public void StartBattle()
        {
            if (hasTriggered) return;
            hasTriggered = true;

            if (GameManager.Instance != null)
            {
                // 1. Save the player's Overworld Position
                var playerObj = GameObject.FindWithTag("Player") ?? GameObject.Find("Player") ?? GameObject.Find("Player_Overworld");
                if (playerObj != null)
                {
                    GameManager.Instance.LastOverworldPosition = playerObj.transform.position;
                    GameManager.Instance.HasSavedOverworldPosition = true;
                }

                // 2. Queue up the Enemies
                GameManager.Instance.PendingEnemyPrefabs.Clear();
                if (enemyPrefabs != null && enemyPrefabs.Count > 0)
                {
                    GameManager.Instance.PendingEnemyPrefabs.AddRange(enemyPrefabs);
                }

                // 3. Load the Battle Scene
                GameManager.Instance.LoadScene(battleSceneName);

                // 4. (Optional) Destroy this trigger so you don't fight it again
                if (destroyAfterTrigger)
                {
                    Destroy(gameObject);
                }
            }
            else
            {
                Debug.LogError("[BattleTrigger] Cannot start battle: GameManager.Instance is missing!");
            }
        }
    }
}
