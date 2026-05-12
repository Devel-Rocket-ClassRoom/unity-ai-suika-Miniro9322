using System;
using SuikaGame.Data;
using UnityEngine;

namespace SuikaGame.Gameplay
{
    /// <summary>
    /// Fruit.OnMergeRequested 를 받아 두 과일을 처리하고, 외부에 머지 결과를 이벤트로 알린다.
    /// 현재는 두 과일을 Destroy 로 회수(풀 #9 도입 시 풀 반환으로 교체).
    /// 다음 단계 과일 스폰은 #14, 점수 가산은 #17, 이펙트는 #16 에서 OnMergeResolved 를 구독해 처리.
    /// 참고: GDD §2-3. 이슈 #13.
    /// </summary>
    [DisallowMultipleComponent]
    public class FruitMerger : MonoBehaviour
    {
        /// <summary>
        /// 머지 처리 완료 후 호출. (mergedLevel, nextStage, midPosition).
        /// nextStage 가 null 이면 최종 단계였음을 의미하지만, 현재 흐름에서는
        /// Fruit.OnCollisionEnter2D 가 최종 단계를 미리 걸러내므로 #15 구현 전엔 발화하지 않는다.
        /// </summary>
        public event Action<int, FruitData, Vector2> OnMergeResolved;

        private void OnEnable()
        {
            Fruit.OnMergeRequested += HandleMergeRequested;
        }

        private void OnDisable()
        {
            Fruit.OnMergeRequested -= HandleMergeRequested;
        }

        private void HandleMergeRequested(Fruit a, Fruit b, Vector2 mid)
        {
            if (a == null || b == null) return;
            if (a.Data == null) return;

            int level = a.Level;
            FruitData nextStage = a.Data.nextStage;

            // 두 과일 회수 (풀 도입 전이라 Destroy)
            ReturnFruit(a);
            ReturnFruit(b);

            // 다음 단계 스폰 / 점수 가산 / 이펙트는 외부에서 처리
            OnMergeResolved?.Invoke(level, nextStage, mid);
        }

        private void ReturnFruit(Fruit f)
        {
            // 풀 도입(#9) 시 이 한 줄을 풀.Despawn(f) 로 교체.
            if (f != null) Destroy(f.gameObject);
        }
    }
}
