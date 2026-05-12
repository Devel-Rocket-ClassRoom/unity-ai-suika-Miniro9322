using System;
using SuikaGame.Data;
using UnityEngine;

namespace SuikaGame.Gameplay
{
    /// <summary>
    /// 박스 안에서 굴러다니는 과일 한 개. FruitData 를 참조해 단계·반지름·스프라이트를 설정한다.
    /// 머지 충돌 로직은 #13 에서 OnCollisionEnter2D 에 추가 예정. 본 이슈(#8)에서는 표현/물리 기본 세팅까지.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class Fruit : MonoBehaviour
    {
        [Header("데이터")]
        [Tooltip("이 과일의 메타데이터. Apply(FruitData, baseRadius) 호출 시 갱신됨.")]
        [SerializeField] private FruitData data;

        [Header("컴포넌트 참조 (자동)")]
        [SerializeField] private CircleCollider2D circle;
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private SpriteRenderer sr;

        /// <summary>현재 적용된 FruitData.</summary>
        public FruitData Data => data;

        /// <summary>FruitData.level (없으면 0).</summary>
        public int Level => data != null ? data.level : 0;

        /// <summary>최종 단계(수박) 여부.</summary>
        public bool IsFinalStage => data != null && data.IsFinalStage;

        /// <summary>
        /// 머지 가능 여부. 머지 직후 일정 시간 / 풀로 반환되기 전까지 false 로 두어 중복 머지 방지.
        /// 기본 true; #13 머지 처리에서 토글한다.
        /// </summary>
        public bool CanMerge { get; set; } = true;

        /// <summary>
        /// 스폰된(또는 풀에서 재활성화된) 시각(Time.time). #19 데스라인 grace 시간 계산에 사용.
        /// Apply() 호출 / OnEnable 시점에 갱신된다.
        /// </summary>
        public float SpawnTime { get; private set; }

        private void Reset()
        {
            CacheComponents();
        }

        private void OnValidate()
        {
            CacheComponents();
        }

        private void Awake()
        {
            CacheComponents();
        }

        private void OnEnable()
        {
            // 풀에서 재활성화될 때도 grace 가 다시 카운트되도록.
            SpawnTime = Time.time;
        }

        private void CacheComponents()
        {
            if (circle == null) circle = GetComponent<CircleCollider2D>();
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (sr == null) sr = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// 데이터 / 표현 일괄 적용. 프리팹 생성 시점과 풀에서 재사용할 때 동일하게 사용.
        /// </summary>
        public void Apply(FruitData newData, float baseRadius)
        {
            data = newData;
            CacheComponents();
            if (data == null) return;

            // CircleCollider2D 반지름
            float worldRadius = Mathf.Max(0.001f, baseRadius * data.relativeRadius);
            circle.radius = worldRadius;

            // 스프라이트
            if (sr != null)
            {
                sr.sprite = data.icon;
                // 스프라이트가 1유닛 = 100픽셀 기준으로 임포트되는 일반적 케이스를 가정.
                // 정확한 시각 크기는 아이콘 임포트 설정에 따라 후속 #24 에서 미세조정.
                // 여기서는 콜라이더 반지름만 정확히 맞추고, 시각은 sprite 기본 크기를 사용.
                sr.transform.localScale = Vector3.one;
            }

            // 이름도 갱신해두면 디버깅 편함
            gameObject.name = $"Fruit_{data.level:00}_{data.nameEn}";

            CanMerge = true;
            SpawnTime = Time.time;
        }

        // ----- 머지 충돌 (#13) -----

        /// <summary>
        /// 동일 레벨 두 과일이 충돌하면 발화. 두 인스턴스 중 GetInstanceID 가 작은 쪽이 발행자.
        /// 두 과일 모두 CanMerge=false 로 잠긴 상태로 전달되므로, 외부(FruitMerger) 가 안전하게 처리하면 됨.
        /// 인자: (lower, higher, midPosition).
        /// </summary>
        public static event Action<Fruit, Fruit, Vector2> OnMergeRequested;

        private void OnCollisionEnter2D(Collision2D col)
        {
            if (!CanMerge) return;
            if (data == null || IsFinalStage) return; // 최종 단계(수박끼리)는 #15에서 별도 처리

            var other = col.collider.GetComponent<Fruit>();
            if (other == null || other == this) return;
            if (!other.CanMerge) return;
            if (data == null || other.data == null) return;
            if (other.Level != Level) return;

            // 두 개 중 GetInstanceID 가 작은 쪽만 머지를 트리거 (중복 방지)
            if (GetInstanceID() > other.GetInstanceID()) return;

            // 양쪽 모두 머지 잠금
            CanMerge = false;
            other.CanMerge = false;

            Vector2 mid = ((Vector2)transform.position + (Vector2)other.transform.position) * 0.5f;
            OnMergeRequested?.Invoke(this, other, mid);
        }
    }
}
