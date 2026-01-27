using UnityEngine;

namespace TowerDefense.Data
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "Tower Defense/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("Basic Info")]
        public string enemyName = "Soldier";
        public GameObject prefab;

        [Header("Base Stats (Wave 1)")]
        public float baseHealth = 30f;  // Scout: 50% less HP
        public float baseSpeed = 2f;
        public int baseKillReward = 10;

        [Header("Scaling Per Wave")]
        [Tooltip("HP multiplier per wave (1.15 = +15%)")]
        public float healthScalePerWave = 1.15f;
        [Tooltip("Speed multiplier per wave (1.02 = +2%)")]
        public float speedScalePerWave = 1.02f;
        [Tooltip("Additional kill reward per wave")]
        public int killRewardPerWave = 1;

        [Header("3D Model")]
        [Tooltip("3D model prefab for this enemy. If null, a placeholder cube will be used.")]
        public GameObject modelPrefab;
        [Tooltip("Scale multiplier for the 3D model (default 0.25 for scouts)")]
        public float modelScale = 0.25f;  // Scout: 75% smaller than original

        public float GetHealthForWave(int wave)
        {
            return baseHealth * Mathf.Pow(healthScalePerWave, wave - 1);
        }

        public float GetSpeedForWave(int wave)
        {
            return baseSpeed * Mathf.Pow(speedScalePerWave, wave - 1);
        }

        public int GetKillRewardForWave(int wave)
        {
            return baseKillReward + (killRewardPerWave * (wave - 1));
        }
    }
}
