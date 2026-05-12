using System;
using SuikaGame.Data;
using UnityEngine;

namespace SuikaGame.Gameplay
{
    /// <summary>
    /// FruitMerger.OnMergeResolved 를 구독해 다음 단계 과일을 중간 지점에 스폰하고,
    /// 머지 결과를 외부에 알리는 OnMerge 이벤트를 발행한다.
    /// 점수(#17 ScoreManager) / 이펙트(#16) / 사운드(#26) 가 OnMerge 를 구독.
    /// 참고: GDD §2-3, §4. 이슈 #14.
    /// </summary>
    [DisallowMultipleComponent]
    public class MergeSpawner : MonoBehaviour
    {
        [Header("참조")]
        [Tooltip("FruitMerger 인스턴스. 같은 씬에 한 개 있다고 가정.")]
        [SerializeField] private FruitMerger merger;

        [Tooltip("스폰된 과일이 부모로 들어갈 컨테이너(선택).")]
        [SerializeField] private Transform spawnParent;

        [Header("연출")]
        [Tooltip("머지 결과 과일에 위쪽으로 살짝 튀는 임펄스 적용.")]
        [SerializeField] private bool applyUpImpulse = true;

        [Tooltip("위로 튀는 임펄스 크기 (Rigidbody2D.AddForce Impulse).")]
        [Min(0f)] [SerializeField] private float upImpulse = 2.0f;

        /// <summary>
        /// 머지 결과 이벤트. (mergedToLevel, position, mergeScore).
        /// ScoreManager / VFX / SFX 가 구독.
        /// </summary>
        public static event Action<int, Vector2, int> OnMerge;

        private void OnEnable()
        {
            if (merger == null)
            {
                merger = FindFirstObjectByType<FruitMerger>();
            }
            if (merger == null)
            {
                Debug.LogError("[MergeSpawner] FruitMerger 가 씬에 없음.");
                enabled = false;
                return;
            }
            merger.OnMergeResolved += HandleMergeResolved;
        }

        private void OnDisable()
        {
            if (merger != null) merger.OnMergeResolved -= HandleMergeResolved;
        }

        private void HandleMergeResolved(int mergedLevel, FruitData nextStage, Vector2 mid)
        {
            // 수박(최종 단계) 머지는 nextStage == null → #15 에서 별도 처리(보너스 점수 + 두 수박 소멸).
            // FruitMerger 단계에서 두 과일은 이미 회수됨.
            if (nextStage == null) return;

            if (nextStage.prefab == null)
            {
                Debug.LogWarning($"[MergeSpawner] {nextStage.name}.prefab 이 비어 있음. Tools/Suika/Generate Fruit Prefabs 실행 필요.");
                return;
            }

            var go = Instantiate(nextStage.prefab, mid, Quaternion.identity, spawnParent);

            var fruit = go.GetComponent<Fruit>();
            if (fruit != null)
            {
                // 프리팹에 동일 데이터가 박혀있긴 하지만, 풀에서 재사용할 때를 대비해 일관되게 Apply.
                fruit.Apply(nextStage, SuikaConstants.BaseFruitRadius);
            }

            if (applyUpImpulse)
            {
                var rb = go.GetComponent<Rigidbody2D>();
                if (rb != null) rb.AddForce(Vector2.up * upImpulse, ForceMode2D.Impulse);
            }

            OnMerge?.Invoke(nextStage.level, mid, nextStage.mergeScore);
        }
    }
}
