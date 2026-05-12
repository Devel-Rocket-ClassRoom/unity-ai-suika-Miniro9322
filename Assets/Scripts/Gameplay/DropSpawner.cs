using System;
using System.Collections;
using SuikaGame.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SuikaGame.Gameplay
{
    [DisallowMultipleComponent]
    public class DropSpawner : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private DropController dropController;
        [SerializeField] private DropPreview preview;
        [SerializeField] private Transform spawnParent;

        [Header("쿨다운")]
        [Min(0f)] [SerializeField] private float cooldownSeconds = 0.5f;

        [Header("대기 과일 (테스트용 폴백)")]
        [SerializeField] private FruitData fallbackFruit;

        private const string MapName = "Gameplay";
        private const string DropActionName = "Drop";
        private InputActionMap gameplayMap;
        private InputAction dropAction;

        public FruitData CurrentFruit
        {
            get => currentFruit;
            set
            {
                currentFruit = value;
                UpdateControllerRadius();
                UpdatePreview();
            }
        }
        private FruitData currentFruit;

        public bool IsDropping { get; private set; }

        public event Action<FruitData, Fruit> OnDropped;
        public static event Action<FruitData, Fruit> OnDroppedStatic;

        public event Action OnReadyForNext;

        private Coroutine cooldownRoutine;

        private void OnEnable()
        {
            if (inputActions == null) { enabled = false; return; }
            gameplayMap = inputActions.FindActionMap(MapName, throwIfNotFound: false);
            dropAction = gameplayMap?.FindAction(DropActionName, throwIfNotFound: false);
            if (dropAction == null) { enabled = false; return; }
            gameplayMap.Enable();
            dropAction.performed += OnDropPerformed;

            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnGameOver += HandleGameOver;

            if (currentFruit == null) currentFruit = fallbackFruit;
            UpdateControllerRadius();
            UpdatePreview();
        }

        private void OnDisable()
        {
            if (dropAction != null) dropAction.performed -= OnDropPerformed;
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnGameOver -= HandleGameOver;
            if (cooldownRoutine != null) StopCoroutine(cooldownRoutine);
            cooldownRoutine = null;
        }

        private void HandleGameOver() => UpdatePreview();

        private void OnDropPerformed(InputAction.CallbackContext _)
        {
            if (!isActiveAndEnabled) return;
            if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameStateManager.State.Playing) return;
            if (IsDropping) return;
            if (dropController == null) return;
            if (currentFruit == null || currentFruit.prefab == null) return;

            SpawnAndStartCooldown();
        }

        private void SpawnAndStartCooldown()
        {
            Vector3 pos = dropController.DropWorldPosition;
            var go = Instantiate(currentFruit.prefab, pos, Quaternion.identity, spawnParent);
            var fruit = go.GetComponent<Fruit>();
            if (fruit != null) fruit.Apply(currentFruit, SuikaConstants.BaseFruitRadius);

            IsDropping = true;
            if (preview != null) preview.SetVisible(false);
            dropController.IsLocked = true;

            OnDropped?.Invoke(currentFruit, fruit);
            OnDroppedStatic?.Invoke(currentFruit, fruit);

            cooldownRoutine = StartCoroutine(CooldownRoutine());
        }

        private IEnumerator CooldownRoutine()
        {
            yield return new WaitForSeconds(cooldownSeconds);
            IsDropping = false;
            UpdatePreview();
            if (dropController != null) dropController.IsLocked = false;
            cooldownRoutine = null;
            OnReadyForNext?.Invoke();
        }

        private void UpdateControllerRadius()
        {
            if (dropController == null || currentFruit == null) return;
            dropController.CurrentFruitRadius = SuikaConstants.BaseFruitRadius * currentFruit.relativeRadius;
        }

        private void UpdatePreview()
        {
            if (preview == null) return;
            bool isGameOver = GameStateManager.Instance != null && GameStateManager.Instance.CurrentState != GameStateManager.State.Playing;
            if (isGameOver || IsDropping || currentFruit == null) preview.SetVisible(false);
            else { preview.SetFruit(currentFruit, SuikaConstants.BaseFruitRadius); preview.SetVisible(true); }
        }
    }
}