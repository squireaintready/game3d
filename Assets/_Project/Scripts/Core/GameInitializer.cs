using UnityEngine;
using TowerDefense.Core;

namespace TowerDefense
{
    public class GameInitializer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameManager gameManager;

        private void Start()
        {
            // Load saved difficulty
            Difficulty difficulty = Difficulty.Normal;

            if (PlayerPrefs.HasKey("SelectedDifficulty"))
            {
                difficulty = (Difficulty)PlayerPrefs.GetInt("SelectedDifficulty");
            }

            // Start the game
            if (gameManager != null)
            {
                gameManager.StartGame(difficulty);
            }
            else if (GameManager.Instance != null)
            {
                GameManager.Instance.StartGame(difficulty);
            }
        }
    }
}
