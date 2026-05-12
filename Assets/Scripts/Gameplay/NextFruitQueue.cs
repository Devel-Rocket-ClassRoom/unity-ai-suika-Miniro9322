using System;
using SuikaGame.Data;
using UnityEngine;

namespace SuikaGame.Gameplay
{
    /// <summary>
    /// 드롭 풀(1~5번) 에서 현재/다음 과일을 보유하는 큐. 길이 2.
    /// 추첨 자체는 DropPoolData.PickRandom() 에 위임 (균등 / 가중치 모두 지원).
    /// DropSpawner.OnReadyForNext 를 구독해 한 칸 진행하고 새 다음 과일을 뽑는다.
    /// 참고: GDD §2-1, §3. 이슈 #12.
    /// </summary>
    [DisallowMultipleComponent]
    public class NextFruitQueue : MonoBehaviour
    {
        [Header("참조")]
        [Tooltip("드롭으로 등장 가능한 과일 풀 (1~5번). #7 에서 생성된 DropPool_1to5 등.")]
        [SerializeField] private DropPoolData dropPool;

        [Tooltip("이 큐에 의해 갱신될 DropSpawner. CurrentFruit 를 채워주고 OnReadyForNext 를 구독.")]
        [SerializeField] private DropSpawner spawner;

        [Header("시드")]
        [Tooltip("0 이상이면 게임 시작 시 해당 시드로 Random.InitState. -1 이면 시드 고정 없이 진행.")]
        [SerializeField] private int initialSeed = -1;

        /// <summary>현재 떨어뜨릴 과일.</summary>
        public FruitData Current { get; private set; }

        /// <summary>다음 떨어뜨릴 과일 (미리보기에 표시).</summary>
        public FruitData Next { get; private set; }

        /// <summary>큐가 갱신될 때 호출. (current, next). UI 미리보기가 구독.</summary>
        public event Action<FruitData, FruitData> OnQueueChanged;

        private bool subscribed;

        private void OnEnable()
        {
            if (dropPool == null)
            {
                Debug.LogError("[NextFruitQueue] DropPoolData 가 비어 있음.");
                enabled = false;
                return;
            }

            ResetQueue();

            if (spawner != null)
            {
                spawner.OnReadyForNext += Advance;
                spawner.CurrentFruit = Current;
                subscribed = true;
            }

            OnQueueChanged?.Invoke(Current, Next);
        }

        private void OnDisable()
        {
            if (subscribed && spawner != null)
            {
                spawner.OnReadyForNext -= Advance;
                subscribed = false;
            }
        }

        /// <summary>큐를 초기 상태로 되돌리고 새 시드 적용. 재시작 시 호출.</summary>
        public void ResetQueue()
        {
            if (initialSeed >= 0)
            {
                UnityEngine.Random.InitState(initialSeed);
            }
            Current = Pick();
            Next = Pick();

            if (spawner != null) spawner.CurrentFruit = Current;
            OnQueueChanged?.Invoke(Current, Next);
        }

        /// <summary>한 칸 진행. 외부에서 강제 호출 가능 (테스트용).</summary>
        public void Advance()
        {
            Current = Next;
            Next = Pick();

            if (spawner != null) spawner.CurrentFruit = Current;
            OnQueueChanged?.Invoke(Current, Next);
        }

        private FruitData Pick()
        {
            var fd = dropPool.PickRandom();
            if (fd == null)
            {
                Debug.LogWarning("[NextFruitQueue] DropPool 에서 과일을 뽑지 못함. 풀이 비어 있는지 확인.");
            }
            return fd;
        }
    }
}
