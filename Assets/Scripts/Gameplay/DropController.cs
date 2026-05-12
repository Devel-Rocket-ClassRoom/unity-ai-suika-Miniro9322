using UnityEngine;
using UnityEngine.InputSystem;

namespace SuikaGame.Gameplay
{
    /// <summary>
    /// 박스 상단의 드롭 포인트를 좌우로 이동시키는 컨트롤러.
    /// 마우스 위치(Pointer 액션) 와 키보드 축(MoveAxis 액션) 중 더 최근에 입력이 들어온 쪽을 우선.
    /// 박스 좌/우 벽 안쪽으로 X 클램프 (현재 대기 과일 반지름 고려).
    /// 낙하 중에는 이동 불가(IsLocked == true).
    ///
    /// 본 컴포넌트는 GameObject 자신의 transform 을 가이드 위치로 사용한다.
    /// 시각화(가이드 아이콘·점선)는 #23 에서 자식으로 붙는다.
    /// 참고: GDD §2-1. 이슈 #10.
    /// </summary>
    [DisallowMultipleComponent]
    public class DropController : MonoBehaviour
    {
        [Header("참조")]
        [Tooltip("박스 컨테이너. 클램프 한계와 좌표계 원점에 사용.")]
        [SerializeField] private BoxContainer box;

        [Tooltip("InputActions.inputactions 자산 (Gameplay 맵 포함).")]
        [SerializeField] private InputActionAsset inputActions;

        [Tooltip("월드→스크린 변환에 사용할 카메라. 비워두면 Camera.main 사용.")]
        [SerializeField] private Camera worldCamera;

        [Header("키보드")]
        [Tooltip("키보드 축 입력 시 좌우 이동 속도 (월드 유닛/초).")]
        [Min(0.1f)] [SerializeField] private float keyboardSpeed = 8f;

        [Header("드롭 포인트 높이")]
        [Tooltip("박스 상단(InteriorTopLocalY) 으로부터 얼마나 위에 드롭 포인트를 둘지 (월드 유닛).")]
        [SerializeField] private float heightAboveBox = 1.0f;

        [Header("입력 우선순위")]
        [Tooltip("마우스가 이 픽셀 이상 움직이면 마우스 입력으로 전환.")]
        [Min(0f)] [SerializeField] private float mouseMoveThreshold = 1f;

        // ----- 액션 캐시 -----
        private const string MapName = "Gameplay";
        private const string PointerActionName = "Pointer";
        private const string MoveAxisActionName = "MoveAxis";

        private InputActionMap gameplayMap;
        private InputAction pointerAction;
        private InputAction moveAxisAction;

        // ----- 상태 -----
        /// <summary>true 면 입력을 무시. 낙하 직후 #11 측에서 일정 시간 true 로 둔다.</summary>
        public bool IsLocked { get; set; }

        /// <summary>현재 대기 과일의 월드 반지름. 클램프 시 이만큼 양쪽에서 빼준다.</summary>
        public float CurrentFruitRadius { get; set; } = SuikaConstants.BaseFruitRadius;

        /// <summary>현재 드롭 X (박스 로컬 좌표).</summary>
        public float CurrentLocalX { get; private set; }

        /// <summary>월드 좌표로 표현한 드롭 포인트.</summary>
        public Vector3 DropWorldPosition
        {
            get
            {
                if (box == null) return transform.position;
                return box.transform.TransformPoint(new Vector3(CurrentLocalX, box.InteriorTopLocalY + heightAboveBox, 0f));
            }
        }

        // 마우스/키보드 우선순위 판정용
        private InputDevice lastInputDevice;
        private Vector2 lastPointerPos;

        // ----- 라이프사이클 -----

        private void Awake()
        {
            if (worldCamera == null) worldCamera = Camera.main;
        }

        private void OnEnable()
        {
            if (inputActions == null)
            {
                Debug.LogError("[DropController] InputActions 자산이 비어 있음.");
                enabled = false;
                return;
            }
            gameplayMap = inputActions.FindActionMap(MapName, throwIfNotFound: false);
            if (gameplayMap == null)
            {
                Debug.LogError($"[DropController] '{MapName}' 액션 맵을 찾지 못함.");
                enabled = false;
                return;
            }
            pointerAction = gameplayMap.FindAction(PointerActionName, throwIfNotFound: false);
            moveAxisAction = gameplayMap.FindAction(MoveAxisActionName, throwIfNotFound: false);
            if (pointerAction == null || moveAxisAction == null)
            {
                Debug.LogError("[DropController] Pointer / MoveAxis 액션을 찾지 못함.");
                enabled = false;
                return;
            }
            gameplayMap.Enable();
            lastPointerPos = pointerAction.ReadValue<Vector2>();
        }

        private void OnDisable()
        {
            if (gameplayMap != null) gameplayMap.Disable();
        }

        // ----- 갱신 -----

        private void Update()
        {
            if (box == null) return;
            if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameStateManager.State.Playing)
            {
                ApplyTransform();
                return;
            }
            if (IsLocked)
            {
                // 잠금 중에도 transform 은 마지막 위치 유지. 시각화는 호출자가 끄도록.
                ApplyTransform();
                return;
            }

            float axis = moveAxisAction.ReadValue<float>();
            Vector2 pointer = pointerAction.ReadValue<Vector2>();
            float pointerDelta = (pointer - lastPointerPos).magnitude;

            // 우선순위: 키보드가 눌려있으면 키보드, 그 외에는 마우스가 충분히 움직였을 때만 마우스.
            bool keyboardActive = Mathf.Abs(axis) > 0.01f;
            bool mouseActive = pointerDelta > mouseMoveThreshold;

            if (keyboardActive)
            {
                CurrentLocalX += axis * keyboardSpeed * Time.deltaTime;
                lastInputDevice = Keyboard.current;
            }
            else if (mouseActive || lastInputDevice == Mouse.current)
            {
                CurrentLocalX = WorldToBoxLocalX(pointer);
                lastInputDevice = Mouse.current;
            }
            // else: 양쪽 모두 idle → CurrentLocalX 유지

            // 클램프
            float limit = Mathf.Max(0f, box.HalfInteriorWidth - CurrentFruitRadius);
            CurrentLocalX = Mathf.Clamp(CurrentLocalX, -limit, limit);

            lastPointerPos = pointer;

            ApplyTransform();
        }

        private void ApplyTransform()
        {
            transform.position = DropWorldPosition;
        }

        /// <summary>스크린 좌표(픽셀) 의 X 를 박스 로컬 X 로 변환.</summary>
        private float WorldToBoxLocalX(Vector2 screen)
        {
            if (worldCamera == null) worldCamera = Camera.main;
            if (worldCamera == null) return CurrentLocalX;

            // 박스가 카메라 평면(=Z 일정) 위에 있다고 가정.
            float z = worldCamera.transform.position.z - box.transform.position.z;
            Vector3 world = worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, Mathf.Abs(z)));
            Vector3 local = box.transform.InverseTransformPoint(world);
            return local.x;
        }

        /// <summary>외부에서 시작 위치를 직접 지정.</summary>
        public void ResetPosition(float localX = 0f)
        {
            CurrentLocalX = localX;
            ApplyTransform();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (box == null) return;
            Vector3 worldPos = DropWorldPosition;
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(worldPos, Mathf.Max(0.1f, CurrentFruitRadius));
            float limit = box.HalfInteriorWidth - CurrentFruitRadius;
            Vector3 leftLimit = box.transform.TransformPoint(new Vector3(-limit, box.InteriorTopLocalY + heightAboveBox, 0f));
            Vector3 rightLimit = box.transform.TransformPoint(new Vector3(limit, box.InteriorTopLocalY + heightAboveBox, 0f));
            Gizmos.color = new Color(1f, 1f, 0.2f, 0.4f);
            Gizmos.DrawLine(leftLimit, rightLimit);
        }
#endif
    }
}
