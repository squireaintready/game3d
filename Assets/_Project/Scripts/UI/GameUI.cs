using UnityEngine;
using UnityEngine.UI;
using TowerDefense.Core;

namespace TowerDefense.UI
{
    public class GameUI : MonoBehaviour
    {
        [Header("Resource Display")]
        [SerializeField] private Text goldText;
        [SerializeField] private Text livesText;
        [SerializeField] private Text waveText;

        [Header("Build Phase")]
        [SerializeField] private Text buildTimerText;
        [SerializeField] private Button startWaveButton;

        [Header("Game Over")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Text gameOverText;
        [SerializeField] private Button restartButton;

        private void OnEnable()
        {
            GameEvents.OnCurrencyChanged += UpdateGold;
            GameEvents.OnLivesChanged += UpdateLives;
            GameEvents.OnWaveStart += UpdateWave;
            GameEvents.OnBuildPhaseTimerUpdate += UpdateBuildTimer;
            GameEvents.OnBuildPhaseStart += ShowBuildPhase;
            GameEvents.OnGameWin += ShowWinScreen;
            GameEvents.OnGameLose += ShowLoseScreen;
        }

        private void OnDisable()
        {
            GameEvents.OnCurrencyChanged -= UpdateGold;
            GameEvents.OnLivesChanged -= UpdateLives;
            GameEvents.OnWaveStart -= UpdateWave;
            GameEvents.OnBuildPhaseTimerUpdate -= UpdateBuildTimer;
            GameEvents.OnBuildPhaseStart -= ShowBuildPhase;
            GameEvents.OnGameWin -= ShowWinScreen;
            GameEvents.OnGameLose -= ShowLoseScreen;
        }

        private void Start()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            if (startWaveButton != null)
            {
                startWaveButton.onClick.AddListener(OnStartWaveClicked);
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }

            // Initial update
            UpdateGold(EconomyManager.Instance?.CurrentCurrency ?? 0);
            UpdateLives(GameManager.Instance?.CurrentLives ?? 0);
            UpdateWave(0);
        }

        private void UpdateGold(int amount)
        {
            if (goldText != null)
            {
                goldText.text = $"Gold: {amount}";
            }
        }

        private void UpdateLives(int lives)
        {
            if (livesText != null)
            {
                livesText.text = $"Lives: {lives}";
            }
        }

        private void UpdateWave(int wave)
        {
            if (waveText != null)
            {
                int totalWaves = GameManager.Instance?.WaveConfig?.totalWaves ?? 15;
                waveText.text = $"Wave: {wave}/{totalWaves}";
            }
        }

        private void UpdateBuildTimer(float time)
        {
            if (buildTimerText != null)
            {
                buildTimerText.text = $"Next Wave: {Mathf.CeilToInt(time)}s";
            }
        }

        private void ShowBuildPhase()
        {
            if (startWaveButton != null)
            {
                startWaveButton.gameObject.SetActive(true);
            }
        }

        private void OnStartWaveClicked()
        {
            WaveManager.Instance?.StartWaveEarly();
            if (startWaveButton != null)
            {
                startWaveButton.gameObject.SetActive(false);
            }
        }

        private void ShowWinScreen()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
            if (gameOverText != null)
            {
                gameOverText.text = "VICTORY!";
            }
        }

        private void ShowLoseScreen()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
            if (gameOverText != null)
            {
                gameOverText.text = "GAME OVER";
            }
        }

        private void OnRestartClicked()
        {
            GameManager.Instance?.RestartGame();
        }
    }
}
