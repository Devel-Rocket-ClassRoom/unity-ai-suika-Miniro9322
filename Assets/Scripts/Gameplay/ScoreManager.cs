using System;
using UnityEngine;

namespace SuikaGame.Gameplay
{
    /// <summary>
    /// 게임 점수 및 하이스코어를 관리하는 싱글톤 스타일 컴포넌트.
    /// MergeSpawner.OnMerge 이벤트를 구독하여 점수를 올린다.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        private const string BestScoreKey = "SuikaBestScore";

        [Header("상태")]
        [SerializeField] private int currentScore = 0;
        [SerializeField] private int bestScore = 0;

        public int CurrentScore => currentScore;
        public int BestScore => bestScore;

        /// <summary>점수 변경 시 호출. (current, best)</summary>
        public event Action<int, int> OnScoreChanged;

        private void Awake()
        {
            bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
        }

        private void OnEnable()
        {
            MergeSpawner.OnMerge += AddScore;
        }

        private void OnDisable()
        {
            MergeSpawner.OnMerge -= AddScore;
        }

        private void AddScore(int level, Vector2 pos, int score)
        {
            currentScore += score;
            
            if (currentScore > bestScore)
            {
                bestScore = currentScore;
                PlayerPrefs.SetInt(BestScoreKey, bestScore);
            }

            OnScoreChanged?.Invoke(currentScore, bestScore);
        }

        /// <summary>게임 재시작 시 호출.</summary>
        public void ResetScore()
        {
            currentScore = 0;
            OnScoreChanged?.Invoke(currentScore, bestScore);
        }
    }
}
