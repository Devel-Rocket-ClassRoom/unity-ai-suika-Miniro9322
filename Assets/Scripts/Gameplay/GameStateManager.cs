using System;
using UnityEngine;

namespace SuikaGame.Gameplay
{
    /// <summary>
    /// 게임 상태(Playing / GameOver) 단일 진입점.
    /// 본 이슈(#19)에서는 GameOver() 호출 + OnGameOver 이벤트 발행까지.
    /// 결과 화면 / 재시작 흐름은 #20 에서 본 컴포넌트를 사용해 구현.
    /// 참고: GDD §2-4, §4.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameStateManager : MonoBehaviour
    {
        public enum State { Playing, GameOver }

        public static GameStateManager Instance { get; private set; }

        /// <summary>현재 상태.</summary>
        public State CurrentState { get; private set; } = State.Playing;

        /// <summary>게임오버 진입 순간 1회 호출.</summary>
        public event Action OnGameOver;

        /// <summary>Playing 으로 복귀(재시작) 순간 호출.</summary>
        public event Action OnRestarted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>게임오버 진입. 이미 GameOver 면 무시.</summary>
        public void GameOver()
        {
            if (CurrentState == State.GameOver) return;
            CurrentState = State.GameOver;
            OnGameOver?.Invoke();
        }

        /// <summary>재시작 상태로 복귀. 실제 리셋 동작은 외부 시스템(NextFruitQueue 등) 이 OnRestarted 를 구독해 처리.</summary>
        public void Restart()
        {
            CurrentState = State.Playing;
            OnRestarted?.Invoke();
        }
    }
}
