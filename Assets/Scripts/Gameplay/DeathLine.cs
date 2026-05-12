using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuikaGame.Gameplay
{
    /// <summary>
    /// 박스 상단의 데스라인. 자식 BoxCollider2D(Trigger) 가 박스 상단부터 위쪽 일정 영역을 덮어,
    /// 그 안에 머무는 과일을 추적한다. 시각화는 LineRenderer 로 반투명 빨간 선.
    /// 누적 시간 카운트 / 게임오버 판정은 #19 (DeathLineGameOver) 에서 본 컴포넌트의 이벤트 / 컬렉션을 사용.
    /// 참고: GDD §2-4, §5. 이슈 #6.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class DeathLine : MonoBehaviour
    {
        [Header("참조")]
        [Tooltip("기준이 되는 박스 컨테이너. 데스라인 Y와 가로 폭을 여기에서 가져온다.")]
        [SerializeField] private BoxContainer box;

        [Header("위치")]
        [Tooltip("박스 상단(InteriorTopLocalY) 로부터의 Y 오프셋. 음수로 두면 박스 내부 살짝 아래.")]
        [SerializeField] private float yOffsetFromTop = 0f;

        [Tooltip("트리거 영역의 세로 두께(=박스 상단 위쪽 어디까지 추적할지). 충분히 크게 두면 안전.")]
        [Min(0.1f)] [SerializeField] private float triggerHeight = 10f;

        [Header("시각화")]
        [Tooltip("LineRenderer 로 빨간 선을 그릴지 여부.")]
        [SerializeField] private bool drawLine = true;

        [Tooltip("선 두께 (월드 유닛).")]
        [Min(0.001f)] [SerializeField] private float lineWidth = 0.06f;

        [Tooltip("선 색상(반투명 빨강 기본).")]
        [SerializeField] private Color lineColor = new Color(1f, 0.2f, 0.2f, 0.6f);

        // ----- 내부 -----
        private const string TriggerChildName = "Trigger";
        private const string LineChildName = "LineVisual";

        private BoxCollider2D triggerCollider;
        private LineRenderer lineRenderer;

        private readonly HashSet<Fruit> overheadFruits = new HashSet<Fruit>();

        /// <summary>현재 데스라인 위에 있는 과일 (외부 read-only).</summary>
        public IReadOnlyCollection<Fruit> OverheadFruits => overheadFruits;

        /// <summary>과일이 데스라인 영역에 진입.</summary>
        public event Action<Fruit> OnFruitEntered;

        /// <summary>과일이 데스라인 영역에서 이탈 (또는 파괴).</summary>
        public event Action<Fruit> OnFruitExited;

        // ----- 라이프사이클 -----

        private void OnEnable()
        {
            EnsureChildren();
            Arrange();
        }

        private void OnValidate()
        {
            if (!isActiveAndEnabled) return;
            EnsureChildren();
            Arrange();
        }

        private void Update()
        {
            // 사라진 Fruit 인스턴스(Destroy 된 경우) 정리. OnTriggerExit2D 가 안 불릴 수 있음.
            if (overheadFruits.Count == 0) return;
            // 빈도 낮추기 위해 매 프레임은 아니라도 일단 가볍게 처리.
            // (HashSet<Fruit> 자체는 작아서 비용은 무시 가능.)
            _toRemoveCache.Clear();
            foreach (var f in overheadFruits)
            {
                if (f == null) _toRemoveCache.Add(f);
            }
            for (int i = 0; i < _toRemoveCache.Count; i++)
            {
                overheadFruits.Remove(_toRemoveCache[i]);
                OnFruitExited?.Invoke(_toRemoveCache[i]);
            }
        }
        private readonly List<Fruit> _toRemoveCache = new List<Fruit>();

        // ----- 자식 보장 + 정렬 -----

        public void EnsureChildren()
        {
            // Trigger
            var trig = transform.Find(TriggerChildName);
            if (trig == null)
            {
                var go = new GameObject(TriggerChildName);
                go.transform.SetParent(transform, false);
                go.AddComponent<DeathLineTriggerRelay>().Owner = this;
                go.AddComponent<BoxCollider2D>();
                trig = go.transform;
            }
            triggerCollider = trig.GetComponent<BoxCollider2D>();
            if (triggerCollider == null) triggerCollider = trig.gameObject.AddComponent<BoxCollider2D>();
            triggerCollider.isTrigger = true;
            var relay = trig.GetComponent<DeathLineTriggerRelay>();
            if (relay == null) relay = trig.gameObject.AddComponent<DeathLineTriggerRelay>();
            relay.Owner = this;

            // Line
            var line = transform.Find(LineChildName);
            if (line == null)
            {
                var go = new GameObject(LineChildName);
                go.transform.SetParent(transform, false);
                go.AddComponent<LineRenderer>();
                line = go.transform;
            }
            lineRenderer = line.GetComponent<LineRenderer>();
            if (lineRenderer == null) lineRenderer = line.gameObject.AddComponent<LineRenderer>();
        }

        public void Arrange()
        {
            if (box == null) return;

            float lineLocalY = box.InteriorTopLocalY + yOffsetFromTop;
            float half = box.HalfInteriorWidth;

            // 트리거 콜라이더: 박스 폭만큼, 데스라인부터 위쪽으로 triggerHeight 만큼.
            if (triggerCollider != null)
            {
                triggerCollider.transform.localPosition = Vector3.zero;
                triggerCollider.transform.localRotation = Quaternion.identity;
                triggerCollider.transform.localScale = Vector3.one;

                triggerCollider.size = new Vector2(half * 2f, triggerHeight);
                triggerCollider.offset = new Vector2(0f, lineLocalY + triggerHeight * 0.5f);
                triggerCollider.isTrigger = true;
            }

            // LineRenderer: 두 점, 박스 좌↔우, 반투명 빨강.
            if (lineRenderer != null)
            {
                lineRenderer.enabled = drawLine;
                lineRenderer.useWorldSpace = false;
                lineRenderer.transform.localPosition = Vector3.zero;
                lineRenderer.transform.localRotation = Quaternion.identity;
                lineRenderer.transform.localScale = Vector3.one;

                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, new Vector3(-half, lineLocalY, 0f));
                lineRenderer.SetPosition(1, new Vector3(+half, lineLocalY, 0f));
                lineRenderer.widthMultiplier = lineWidth;
                lineRenderer.startWidth = lineWidth;
                lineRenderer.endWidth = lineWidth;

                // Sprites-Default 셰이더면 알파 지원. URP 2D에서도 정상 동작.
                if (lineRenderer.sharedMaterial == null)
                {
                    var mat = new Material(Shader.Find("Sprites/Default"));
                    lineRenderer.sharedMaterial = mat;
                }
                lineRenderer.startColor = lineColor;
                lineRenderer.endColor = lineColor;
                lineRenderer.numCapVertices = 2;
            }
        }

        // ----- 트리거 핸들러 (relay 로부터 호출) -----

        internal void HandleEnter(Collider2D other)
        {
            var fruit = other.GetComponentInParent<Fruit>();
            if (fruit == null) return;
            if (overheadFruits.Add(fruit))
            {
                OnFruitEntered?.Invoke(fruit);
            }
        }

        internal void HandleExit(Collider2D other)
        {
            var fruit = other.GetComponentInParent<Fruit>();
            if (fruit == null) return;
            if (overheadFruits.Remove(fruit))
            {
                OnFruitExited?.Invoke(fruit);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (box == null) return;
            float lineLocalY = box.InteriorTopLocalY + yOffsetFromTop;
            float half = box.HalfInteriorWidth;
            Vector3 left = box.transform.TransformPoint(new Vector3(-half, lineLocalY, 0f));
            Vector3 right = box.transform.TransformPoint(new Vector3(+half, lineLocalY, 0f));
            Gizmos.color = lineColor;
            Gizmos.DrawLine(left, right);

            // 추적 영역 와이어큐브
            Gizmos.color = new Color(lineColor.r, lineColor.g, lineColor.b, 0.2f);
            Vector3 c = box.transform.TransformPoint(new Vector3(0f, lineLocalY + triggerHeight * 0.5f, 0f));
            Vector3 s = new Vector3(half * 2f, triggerHeight, 0.1f);
            Gizmos.DrawWireCube(c, s);
        }
#endif
    }

    /// <summary>
    /// 자식 트리거 콜라이더에서 발생한 충돌 이벤트를 부모 DeathLine 으로 전달하는 릴레이.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    internal class DeathLineTriggerRelay : MonoBehaviour
    {
        public DeathLine Owner;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (Owner != null) Owner.HandleEnter(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (Owner != null) Owner.HandleExit(other);
        }
    }
}
