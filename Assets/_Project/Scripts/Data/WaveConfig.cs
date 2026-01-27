using UnityEngine;
using TowerDefense.Core;

namespace TowerDefense.Data
{
    [CreateAssetMenu(fileName = "WaveConfig", menuName = "Tower Defense/Wave Config")]
    public class WaveConfig : ScriptableObject
    {
        [Header("General Settings")]
        public int totalWaves = 15;
        public float buildPhaseDuration = 30f;
        public float maxEarlyStartBonus = 50f;

        [Header("Economy")]
        public int roundSurvivalBonusBase = 25;
        public int roundSurvivalBonusPerWave = 5;

        [Header("Wave Definitions")]
        public WaveDefinition[] waves;

        [Header("Difficulty Multipliers")]
        public DifficultySettings easySettings;
        public DifficultySettings normalSettings;
        public DifficultySettings hardSettings;

        public WaveDefinition GetWaveDefinition(int waveNumber)
        {
            if (waves != null && waveNumber > 0 && waveNumber <= waves.Length)
            {
                return waves[waveNumber - 1];
            }
            return GenerateDefaultWave(waveNumber);
        }

        private WaveDefinition GenerateDefaultWave(int wave)
        {
            var def = new WaveDefinition();

            // Enemy counts doubled for smaller scout units
            if (wave == 1)
            {
                def.enemyCount = 10;  // Was 5
                def.spawnRate = 1.5f; // Slightly faster spawn for more enemies
            }
            else if (wave <= 5)
            {
                def.enemyCount = (8 + (wave * 2)) * 2;  // Doubled
                def.spawnRate = 1.8f;
            }
            else if (wave <= 10)
            {
                def.enemyCount = (15 + (wave * 2)) * 2;  // Doubled
                def.spawnRate = 2.5f;
            }
            else
            {
                def.enemyCount = (25 + (wave * 3)) * 2;  // Doubled
                def.spawnRate = 3f;
            }

            return def;
        }

        public DifficultySettings GetDifficultySettings(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Easy:
                    return easySettings;
                case Difficulty.Hard:
                    return hardSettings;
                default:
                    return normalSettings;
            }
        }

        public int GetRoundSurvivalBonus(int wave)
        {
            return roundSurvivalBonusBase + (wave * roundSurvivalBonusPerWave);
        }

        public int GetEarlyStartBonus(float secondsRemaining)
        {
            float ratio = secondsRemaining / buildPhaseDuration;
            return Mathf.FloorToInt(ratio * maxEarlyStartBonus);
        }
    }

    [System.Serializable]
    public class WaveDefinition
    {
        public int enemyCount = 10;
        public float spawnRate = 1f;
        public EnemyData enemyType;
    }

    [System.Serializable]
    public class DifficultySettings
    {
        [Range(0.5f, 2f)]
        public float enemyHealthMultiplier = 1f;
        [Range(0.5f, 2f)]
        public float enemySpeedMultiplier = 1f;
        [Range(0.5f, 2f)]
        public float enemyCountMultiplier = 1f;
    }
}
