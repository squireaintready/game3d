using UnityEngine;
using TowerDefense.Core;

namespace TowerDefense.Data
{
    [CreateAssetMenu(fileName = "TowerData", menuName = "Tower Defense/Tower Data")]
    public class TowerData : ScriptableObject
    {
        [Header("Basic Info")]
        public string towerName;
        public TowerType towerType;
        public Sprite icon;
        public GameObject prefab;

        [Header("Economy")]
        public int cost = 75;
        [Range(0f, 1f)]
        public float sellRefundPercent = 0.75f;

        [Header("Combat Stats")]
        public float damage = 15f;
        public float range = 2.5f;
        public float attackSpeed = 1.5f;
        public float projectileSpeed = 8f;

        [Header("3D Model - Simple")]
        [Tooltip("3D model prefab for this tower. If null, a placeholder cube will be used.")]
        public GameObject modelPrefab;
        [Tooltip("Scale multiplier for the 3D model (default 1.0)")]
        public float modelScale = 1.0f;
        [Tooltip("Non-uniform scale override (X=width, Y=height, Z=depth). If non-zero, overrides modelScale.")]
        public Vector3 modelScaleVector = Vector3.zero;
        [Tooltip("Y rotation offset for the model in degrees")]
        public float modelRotationOffset = 0f;

        [Header("3D Model - Composite (Wall + Unit)")]
        [Tooltip("Wall/base prefab for composite towers (e.g., castle wall segment)")]
        public GameObject wallPrefab;
        [Tooltip("Unit prefab to place on top of wall (e.g., soldier, mage, cannon)")]
        public GameObject unitPrefab;
        [Tooltip("Number of units to spawn on top (e.g., 2 archers, 1 mage)")]
        public int unitCount = 1;
        [Tooltip("Position offset for units relative to wall top")]
        public Vector3 unitOffset = new Vector3(0f, 0.5f, 0f);
        [Tooltip("Scale for the wall model")]
        public float wallScale = 1.0f;
        [Tooltip("Non-uniform scale for wall (X=width, Y=height, Z=depth). If non-zero, overrides wallScale.")]
        public Vector3 wallScaleVector = Vector3.zero;
        [Tooltip("Scale for the unit model(s)")]
        public float unitScale = 1.0f;

        public bool IsAttackTower => towerType != TowerType.Wall;

        // Upgrade system constants
        public const int MaxLevel = 4;
        private static readonly float[] UpgradeCostPercents = { 0.5f, 0.75f, 1.0f }; // Level 2, 3, 4
        private const float DamageIncreasePerLevel = 0.15f; // +15% per level
        private const float RangeIncreasePerLevel = 0.05f;  // +5% per level

        public int GetSellValue()
        {
            return Mathf.FloorToInt(cost * sellRefundPercent);
        }

        /// <summary>
        /// Get sell value for a tower at a specific level (includes upgrade costs)
        /// </summary>
        public int GetSellValue(int level)
        {
            // Walls always sell for 100%
            if (towerType == TowerType.Wall)
            {
                return cost;
            }

            // Calculate total invested (base cost + all upgrades)
            int totalInvested = cost;
            for (int i = 1; i < level; i++)
            {
                totalInvested += GetUpgradeCost(i);
            }
            return Mathf.FloorToInt(totalInvested * sellRefundPercent);
        }

        /// <summary>
        /// Get the cost to upgrade from currentLevel to next level
        /// </summary>
        public int GetUpgradeCost(int currentLevel)
        {
            if (currentLevel < 1 || currentLevel >= MaxLevel)
                return 0;

            // Array is 0-indexed: level 1→2 uses index 0, level 2→3 uses index 1, etc.
            int index = currentLevel - 1;
            if (index >= UpgradeCostPercents.Length)
                return 0;

            return Mathf.FloorToInt(cost * UpgradeCostPercents[index]);
        }

        /// <summary>
        /// Get damage value at a specific level
        /// </summary>
        public float GetDamageForLevel(int level)
        {
            if (level <= 1) return damage;
            // Each level adds 15% multiplicatively
            return damage * Mathf.Pow(1f + DamageIncreasePerLevel, level - 1);
        }

        /// <summary>
        /// Get range value at a specific level
        /// </summary>
        public float GetRangeForLevel(int level)
        {
            if (level <= 1) return range;
            // Each level adds 5% multiplicatively
            return range * Mathf.Pow(1f + RangeIncreasePerLevel, level - 1);
        }

        /// <summary>
        /// Check if tower can be upgraded (only attack towers)
        /// </summary>
        public bool CanUpgrade => IsAttackTower;
    }
}
