using System.Collections.Generic;
using UnityEngine;

namespace SuikaGame.Gameplay
{
    /// <summary>
    /// DeathLine 위에 머무는 과일별로 누적 시간을 측정해 임계치(기본 3초)를 넘기면 게임오버 트리거.
    /// grace 시간: 낙하/머지 직후 잠시는 카운트에 포함하지 않음 (Fruit.SpawnTime + spawnGrace 까지 무시).
    /// 디버그 토글 시 Scene 뷰에 각 과일의 남은 시간을 표시.
    /// 참고: GDD §2-4, §4. 이슈 #19.
    /// </summary>
    [DisallowMultipleComponent]
    public class DeathLineWatcher : MonoBehaviour
    {
        [Header("참조")]
        [Tooltip("DeathLine. 비어 있으면 씬에서 자동 탐색.")]
        [SerializeField] private DeathLine deathLine;

        [Tooltip("GameStateManager. 비어 있으면 Singleton 인스턴스를 사용.")]
        [SerializeField] private GameStateManager gameState;

        [Header("임계치 / Grace")]
        [Tooltip("데스라인 위 누적 시간이 이 값 이상이면 게임오버. GDD §2-4 기본 3초.")]
        [Min(0.1f)] [SerializeField] private float threshold = 3f;

        [Tooltip("Fruit.SpawnTime 으로부터 이 만큼 지나기 전까지는 데스라인 카운트 미시작.")]
        [Min(0f)] [SerializeField] private float spawnGrace = 0.6f;

        [Header("디버그")]
        [Tooltip("Scene 뷰에 데스라인 위 과일의 남은 시간을 표시.")]
        [SerializeField] private bool showDebugLabels = false;

        private class Entry { public float enterTime; }

        private readonly Dictionary<Fruit, Entry> tracked = new Dictionary<Fruit, Entry>();
        private readonly List<Fruit> _cleanupCache = new List<Fruit>();

        private bool subscribed;

        // ----- 라이프사이클 -----

        private void OnEnable()
        {
            if (deathLine == null) deathLine = FindFirstObjectByType<DeathLine>();
            if (deathLine == null)
            {
                Debug.LogError("[DeathLineWatcher] DeathLine 이 씬에 없음.");
                enabled = false;
                return;
            }
            deathLine.OnFruitEntered += HandleEntered;
            deathLine.OnFruitExited += HandleExited;
            subscribed = true;
        }

        private void OnDisable()
        {
            if (subscribed && deathLine != null)
            {
                deathLine.OnFruitEntered -= HandleEntered;
                deathLine.OnFruitExited -= HandleExited;
                subscribed = false;
            }
            tracked.Clear();
        }

        private void HandleEntered(Fruit f)
        {
            if (f == null) return;
            if (!tracked.ContainsKey(f))
            {
                tracked.Add(f, new Entry { enterTime = Time.time });
            }
        }

        private void HandleExited(Fruit f)
        {
            if (f == null) return;
            tracked.Remove(f);
        }

        private void Update()
        {
            var gs = ResolveGameState();
            if (gs != null && gs.CurrentState != GameStateManager.State.Playing) return;
            if (tracked.Count == 0) return;

            // null 정리 (Fruit Destroy 직후 Exit 누락 안전망)
            _cleanupCache.Clear();
            foreach (var kv in tracked)
            {
                if (kv.Key == null) _cleanupCache.Add(kv.Key);
            }
            for (int i = 0; i < _cleanupCache.Count; i++)
            {
                tracked.Remove(_cleanupCache[i]);
            }

            // 임계치 체크
            float now = Time.time;
            foreach (var kv in tracked)
            {
                var f = kv.Key;
                if (f == null) continue;
                float dangerStart = Mathf.Max(kv.Value.enterTime, f.SpawnTime + spawnGrace);
                if (now - dangerStart >= threshold)
                {
                    TriggerGameOver();
                    return;
                }
            }
        }

        private void TriggerGameOver()
        {
            var gs = ResolveGameState();
            if (gs == null)
            {
                Debug.LogWarning("[DeathLineWatcher] GameStateManager 가 없음. 게임오버 트리거 불가.");
                return;
            }
            gs.GameOver();
        }

        private GameStateManager ResolveGameState()
        {
            if (gameState != null) return gameState;
            gameState = GameStateManager.Instance;
            if (gameState == null) gameState = FindFirstObjectByType<GameStateManager>();
            return gameState;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showDebugLabels) return;
            if (!Application.isPlaying) return;
            float now = Time.time;
            foreach (var kv in tracked)
            {
                var f = kv.Key;
                if (f == null) continue;
                float dangerStart = Mathf.Max(kv.Value.enterTime, f.SpawnTime + spawnGrace);
                float elapsed = now - dangerStart;
                float remaining = Mathf.Max(0f, threshold - elapsed);
                Vector3 p = f.transform.position + Vector3.up * 0.4f;
                UnityEditor.Handles.color = remaining < 1f ? Color.red : Color.yellow;
                UnityEditor.Handles.Label(p, $"{remaining:0.00}s");
            }
        }
#endif
    }
}
