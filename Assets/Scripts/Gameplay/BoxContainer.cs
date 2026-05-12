using UnityEngine;

namespace SuikaGame.Gameplay
{
    /// <summary>
    /// 박스 컨테이너 — 바닥 / 좌벽 / 우벽 정적 콜라이더로 과일이 쌓이는 공간.
    /// 너비/높이/벽 두께를 직렬화 필드로 노출하고, 자식 콜라이더의 위치·크기를 자동 정렬한다.
    /// 자식 트랜스폼 이름은 "Floor", "LeftWall", "RightWall" 로 고정.
    /// 참고: GDD §2-2, §5.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class BoxContainer : MonoBehaviour
    {
        [Header("크기 (월드 유닛)")]
        [Tooltip("박스 내부 가로 너비 (벽 안쪽 기준).")]
        [Min(0.1f)] public float interiorWidth = 6f;

        [Tooltip("박스 내부 세로 높이 (바닥 윗면 ~ 박스 상단).")]
        [Min(0.1f)] public float interiorHeight = 10f;

        [Tooltip("벽 / 바닥의 두께. 콜라이더가 바깥쪽으로 두꺼워진다.")]
        [Min(0.01f)] public float wallThickness = 0.5f;

        [Header("물리 머티리얼 (선택)")]
        [Tooltip("벽 / 바닥에 적용할 PhysicsMaterial2D. 비워두면 머티리얼 미할당.")]
        public PhysicsMaterial2D wallMaterial;

        [Header("자동 정렬")]
        [Tooltip("값이 바뀌면 자식 콜라이더를 자동 갱신.")]
        public bool autoArrangeOnValidate = true;

        // 자식 이름 규약
        private const string FloorName = "Floor";
        private const string LeftWallName = "LeftWall";
        private const string RightWallName = "RightWall";

        /// <summary>박스 내부 X 좌우 한계 (월드 기준이 아니라 로컬 기준).</summary>
        public float HalfInteriorWidth => interiorWidth * 0.5f;

        /// <summary>박스 내부 Y 최저값 (바닥 윗면 = 0, 박스 상단 = interiorHeight).</summary>
        public float InteriorBottomLocalY => 0f;
        public float InteriorTopLocalY => interiorHeight;

        private void OnEnable()
        {
            // 빌드 / 플레이 시작 시 한 번 보장
            EnsureChildren();
            Arrange();
        }

        private void OnValidate()
        {
            if (!autoArrangeOnValidate) return;
            // 인스펙터에서 값 바뀌면 정렬. 단, 프리팹 에디트 모드 등에서 즉시 자식 추가는 안전을 위해 보류.
            if (!isActiveAndEnabled) return;
            EnsureChildren();
            Arrange();
        }

        /// <summary>벽/바닥 GameObject가 없으면 만든다.</summary>
        public void EnsureChildren()
        {
            EnsureChild(FloorName);
            EnsureChild(LeftWallName);
            EnsureChild(RightWallName);
        }

        private Transform EnsureChild(string childName)
        {
            var t = transform.Find(childName);
            if (t == null)
            {
                var go = new GameObject(childName);
                go.transform.SetParent(transform, false);
                go.AddComponent<BoxCollider2D>();
                t = go.transform;
            }
            if (t.GetComponent<BoxCollider2D>() == null)
            {
                t.gameObject.AddComponent<BoxCollider2D>();
            }
            return t;
        }

        /// <summary>현재 사이즈에 맞춰 자식 콜라이더의 위치·크기를 갱신.</summary>
        public void Arrange()
        {
            var floor = transform.Find(FloorName);
            var left = transform.Find(LeftWallName);
            var right = transform.Find(RightWallName);
            if (floor == null || left == null || right == null) return;

            float half = HalfInteriorWidth;
            float t = wallThickness;

            // 바닥: 가로 = 내부 너비 + 양쪽 벽 두께 (벽 바깥까지 덮음), 세로 = 두께
            ConfigureWall(floor,
                pos:    new Vector2(0f, -t * 0.5f),
                size:   new Vector2(interiorWidth + t * 2f, t));

            // 좌벽: 세로 = 내부 높이, 윗단이 박스 상단과 일치하도록 배치
            ConfigureWall(left,
                pos:    new Vector2(-(half + t * 0.5f), interiorHeight * 0.5f),
                size:   new Vector2(t, interiorHeight));

            // 우벽
            ConfigureWall(right,
                pos:    new Vector2(half + t * 0.5f, interiorHeight * 0.5f),
                size:   new Vector2(t, interiorHeight));
        }

        private void ConfigureWall(Transform t, Vector2 pos, Vector2 size)
        {
            t.localPosition = (Vector3)pos;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;

            var box = t.GetComponent<BoxCollider2D>();
            if (box == null) box = t.gameObject.AddComponent<BoxCollider2D>();
            box.offset = Vector2.zero;
            box.size = size;
            if (wallMaterial != null) box.sharedMaterial = wallMaterial;

            // 시각적 요소(SpriteRenderer)가 있다면 크기 동기화
            var sr = t.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = size;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.6f);
            Vector3 c = transform.position + new Vector3(0f, interiorHeight * 0.5f, 0f);
            Vector3 s = new Vector3(interiorWidth, interiorHeight, 0f);
            Gizmos.DrawWireCube(c, s);
        }
#endif
    }
}
