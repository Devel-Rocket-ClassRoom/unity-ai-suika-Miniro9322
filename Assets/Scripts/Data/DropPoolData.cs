using System.Collections.Generic;
using UnityEngine;

namespace SuikaGame.Data
{
    /// <summary>
    /// 드롭 풀(1~5번)을 모아둔 ScriptableObject.
    /// 가중치를 별도로 두어 균등(20%)·낮은 번호 가중 양쪽 모두 표현 가능.
    /// 가중치 합이 0이면 균등 확률로 폴백.
    /// 참고: GDD §2-1, §3 드롭 풀.
    /// </summary>
    [CreateAssetMenu(fileName = "DropPoolData", menuName = "Suika/Drop Pool Data", order = 11)]
    public class DropPoolData : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            [Tooltip("드롭 풀에 포함되는 과일 데이터 (1~5번 기대).")]
            public FruitData fruit;

            [Tooltip("이 과일의 등장 가중치. 모든 가중치가 0이면 균등 확률.")]
            [Min(0f)] public float weight;
        }

        [Tooltip("드롭으로 등장 가능한 과일 목록과 가중치.")]
        public List<Entry> entries = new List<Entry>();

        /// <summary>가중치 합. 0이면 균등 확률 분기에 사용한다.</summary>
        public float TotalWeight
        {
            get
            {
                float sum = 0f;
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i].fruit == null) continue;
                    sum += Mathf.Max(0f, entries[i].weight);
                }
                return sum;
            }
        }

        /// <summary>
        /// 가중치/균등 확률에 따라 과일 하나를 뽑는다.
        /// 비어 있으면 null 반환.
        /// </summary>
        public FruitData PickRandom()
        {
            if (entries == null || entries.Count == 0) return null;

            float total = TotalWeight;
            if (total <= 0f)
            {
                // 균등 확률 폴백
                int idx = Random.Range(0, entries.Count);
                return entries[idx].fruit;
            }

            float r = Random.value * total;
            float acc = 0f;
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e.fruit == null) continue;
                acc += Mathf.Max(0f, e.weight);
                if (r <= acc) return e.fruit;
            }
            // 부동소수 누락 보정
            return entries[entries.Count - 1].fruit;
        }
    }
}
