using System;
using System.Collections;
using SuikaGame.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SuikaGame.Gameplay
{
    /// <summary>
    /// Drop 입력을 받아 현재 대기 과일을 드롭 포인트 위치에 스폰한다.
    /// 스폰 직후 IsDropping = true, DropController.IsLocked = true 로 입력 잠금.
    /// 약 0.5초 후 잠금 해제 + OnReadyForNext 이벤트 발행 — #12 NextFruitQueue 가 다음 과일을 채워준다.
    /// 참고: GDD §2-1. 이슈 #11.
    /// </summary>
    [DisallowMultipleComponent]
    public class DropSpawner : MonoBehaviour
    {
        [Header("참조")]
        [Tooltip("Drop 액션이 들어있는 InputActions 자산.")]
        [SerializeField] private InputActionAsset inputActions;

        [Tooltip("드롭 위치를 알려주고 잠금 플래그를 공유하는 컨트롤러.")]
        [SerializeField] private DropController dropController;

        [Tooltip("스폰된 과일이 부모로 들어갈 컨테이너(선택). 비어 있으면 부모 없이 월드에 스폰.")]
        [SerializeField] private Transform spawnParent;

        [Header("쿨다운")]
        [Tooltip("드롭 직후 다음 입력까지의 딜레이(초). GDD §2-1.")]
        [Min(0f)] [SerializeField] private float cooldownSeconds = 0.5f;

        [Header("대기 과일 (테스트용 폴백)")]
        [Tooltip("NextFruitQueue 가 없을 때 사용할 기본 대기 과일. #12 머지 후엔 queue 가 갱신한다.")]
        [SerializeField] private FruitData fallbackFruit;

        // ----- 액션 -----
        private const string MapName = "Gameplay";
        private const string DropActionName = "Drop";
        private InputActionMap gameplayMap;
        private InputAction dropAction;

        // ----- 상태 -----
        /// <summary>현재 대기 과일. NextFruitQueue 가 갱신한다.</summary>
        public FruitData CurrentFruit
        {
            get => currentFruit;
            set
            {
                currentFruit = value;
                UpdateControllerRadius();
            }
        }
        private FruitData currentFruit;

        /// <summary>스폰 직후 ~ 쿨다운 종료 사이.</summary>
        public bool IsDropping { get; private set; }

        /// <summary>드롭 직후 호출. 두 번째 인자는 스폰된 Fruit.</summary>
        public event Action<FruitData, Fruit> OnDropped;

        /// <summary>쿨다운 종료 후 다음 과일을 채워달라는 신호. #12 가 구독한다.</summary>
        public event Action OnReadyForNext;

        private Coroutine cooldownRoutine;

        // ----- 라이프사이클 -----

        private void OnEnable()
        {
            if (inputActions == null)
            {
                Debug.LogError("[DropSpawner] InputActions 자산이 비어 있음.");
                enabled = false;
                return;
            }
            gameplayMap = inputActions.FindActionMap(MapName, throwIfNotFound: false);
            dropAction = gameplayMap?.FindAction(DropActionName, throwIfNotFound: false);
            if (dropAction == null)
            {
                Debug.LogError("[DropSpawner] Drop 액션을 찾지 못함.");
                enabled = false;
                return;
            }
            gameplayMap.Enable();
            dropAction.performed += OnDropPerformed;

            if (currentFruit == null) currentFruit = fallbackFruit;
            UpdateControllerRadius();
        }

        private void OnDisable()
        {
            if (dropAction != null) dropAction.performed -= OnDropPerformed;
            if (cooldownRoutine != null) StopCoroutine(cooldownRoutine);
            cooldownRoutine = null;
        }

        // ----- 핸들러 -----

        private void OnDropPerformed(InputAction.CallbackContext _)
        {
            if (!isActiveAndEnabled) return;
            if (IsDropping) return;
            if (dropController == null) return;
            if (currentFruit == null)
            {
                Debug.LogWarning("[DropSpawner] CurrentFruit 가 비어 있어 드롭 불가.");
                return;
            }
            if (currentFruit.prefab == null)
            {
                Debug.LogWarning($"[DropSpawner] {currentFruit.name}.prefab 이 비어 있음. FruitPrefabGenerator 실행 필요.");
                return;
            }

            SpawnAndStartCooldown();
        }

        // ----- 스폰 -----

        private void SpawnAndStartCooldown()
        {
            Vector3 pos = dropController.DropWorldPosition;
            var go = Instantiate(currentFruit.prefab, pos, Quaternion.identity, spawnParent);
            var fruit = go.GetComponent<Fruit>();
            if (fruit != null)
            {
                // 프리팹에 이미 같은 데이터가 박혀있긴 하지만, 풀에서 재사용할 때를 대비해 일관되게 Apply 호출.
                fruit.Apply(currentFruit, SuikaConstants.BaseFruitRadius);
            }

            IsDropping = true;
            dropController.IsLocked = true;

            OnDropped?.Invoke(currentFruit, fruit);

            cooldownRoutine = StartCoroutine(CooldownRoutine());
        }

        private IEnumerator CooldownRoutine()
        {
            yield return new WaitForSeconds(cooldownSeconds);
            IsDropping = false;
            if (dropController != null) dropController.IsLocked = false;
            cooldownRoutine = null;

            OnReadyForNext?.Invoke();
        }

        private void UpdateControllerRadius()
        {
            if (dropController == null || currentFruit == null) return;
            dropController.CurrentFruitRadius = SuikaConstants.BaseFruitRadius * currentFruit.relativeRadius;
        }
    }
}
