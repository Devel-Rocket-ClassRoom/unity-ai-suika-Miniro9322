using System.Collections.Generic;
using SuikaGame.Data;
using SuikaGame.Gameplay;
using SuikaGame.Audio;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace SuikaGame.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class MainGameUIController : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private NextFruitQueue nextFruitQueue;
        [SerializeField] private string fruitDataFolder = "Assets/Data/Fruits";

        private Label currentScoreLabel;
        private Label bestScoreLabel;
        private VisualElement nextIcon;
        private VisualElement evolutionList;

        private VisualElement gameOverOverlay;
        private Label finalScoreLabel;
        private Label finalBestScoreLabel;
        private Button retryButton;
        private Button mainButton;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            currentScoreLabel = root.Q<Label>("currentScore");
            bestScoreLabel = root.Q<Label>("bestScore");
            nextIcon = root.Q<VisualElement>("nextIcon");
            evolutionList = root.Q<VisualElement>("evolutionList");

            gameOverOverlay = root.Q<VisualElement>("gameOverOverlay");
            finalScoreLabel = root.Q<Label>("finalScore");
            finalBestScoreLabel = root.Q<Label>("finalBestScore");
            retryButton = root.Q<Button>("retryButton");
            mainButton = root.Q<Button>("mainButton");

            if (retryButton != null) retryButton.clicked += OnRetryClicked;
            if (mainButton != null) mainButton.clicked += OnMainClicked;

            if (scoreManager != null)
            {
                scoreManager.OnScoreChanged += UpdateScore;
            }

            if (nextFruitQueue != null)
            {
                nextFruitQueue.OnQueueChanged += UpdateNextFruit;
            }

            var gs = ResolveGameState();
            if (gs != null)
            {
                gs.OnGameOver += ShowGameOver;
                gs.OnRestarted += HideGameOver;
            }

            PopulateEvolutionChart();
        }

        private void Start()
        {
            if (scoreManager != null)
            {
                UpdateScore(scoreManager.CurrentScore, scoreManager.BestScore);
            }

            if (nextFruitQueue != null)
            {
                UpdateNextFruit(nextFruitQueue.Current, nextFruitQueue.Next);
            }
        }

        private void OnDisable()
        {
            if (scoreManager != null) scoreManager.OnScoreChanged -= UpdateScore;
            if (nextFruitQueue != null) nextFruitQueue.OnQueueChanged -= UpdateNextFruit;

            var gs = ResolveGameState();
            if (gs != null)
            {
                gs.OnGameOver -= ShowGameOver;
                gs.OnRestarted -= HideGameOver;
            }

            if (retryButton != null) retryButton.clicked -= OnRetryClicked;
            if (mainButton != null) mainButton.clicked -= OnMainClicked;
        }

        private GameStateManager ResolveGameState()
        {
            if (GameStateManager.Instance != null) return GameStateManager.Instance;
            return Object.FindFirstObjectByType<GameStateManager>();
        }

        private void UpdateScore(int current, int best)
        {
            if (currentScoreLabel != null) currentScoreLabel.text = current.ToString("N0");
            if (bestScoreLabel != null) bestScoreLabel.text = best.ToString("N0");
        }

        private void UpdateNextFruit(FruitData current, FruitData next)
        {
            if (nextIcon != null && next != null)
            {
                nextIcon.style.backgroundImage = new StyleBackground(next.icon);
            }
            else if (nextIcon != null)
            {
                nextIcon.style.backgroundImage = null;
            }
        }

        private void ShowGameOver()
        {
            if (gameOverOverlay == null) return;

            if (scoreManager != null)
            {
                if (finalScoreLabel != null) finalScoreLabel.text = scoreManager.CurrentScore.ToString("N0");
                if (finalBestScoreLabel != null) finalBestScoreLabel.text = scoreManager.BestScore.ToString("N0");
            }

            gameOverOverlay.style.display = DisplayStyle.Flex;
        }

        private void HideGameOver()
        {
            if (gameOverOverlay != null)
            {
                gameOverOverlay.style.display = DisplayStyle.None;
            }
        }

        private void OnRetryClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.Restart();
            }
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnMainClicked()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
            Debug.Log("Go to Main Menu");
        }

        private void PopulateEvolutionChart()
        {
            if (evolutionList == null) return;
            evolutionList.Clear();

            #if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:FruitData", new[] { fruitDataFolder });
            List<FruitData> fruits = new List<FruitData>();
            foreach (var guid in guids)
            {
                var data = UnityEditor.AssetDatabase.LoadAssetAtPath<FruitData>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                if (data != null) fruits.Add(data);
            }
            fruits.Sort((a, b) => a.level.CompareTo(b.level));

            for (int i = 0; i < fruits.Count; i++)
            {
                var item = new VisualElement();
                item.AddToClassList("evolution-item");

                var icon = new VisualElement();
                icon.AddToClassList("evolution-item-icon");
                icon.style.backgroundImage = new StyleBackground(fruits[i].icon);
                item.Add(icon);

                if (i < fruits.Count - 1)
                {
                    var arrow = new Label("▸");
                    arrow.AddToClassList("evolution-item-arrow");
                    item.Add(arrow);
                }

                var label = new Label(fruits[i].nameKo);
                label.AddToClassList("evolution-item-label");
                item.Add(label);

                evolutionList.Add(item);
            }
            #endif
        }
    }
}