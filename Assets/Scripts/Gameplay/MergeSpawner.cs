using System;
using SuikaGame.Data;
using UnityEngine;

namespace SuikaGame.Gameplay
{
    [DisallowMultipleComponent]
    public class MergeSpawner : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private FruitMerger merger;
        [SerializeField] private Transform spawnParent;

        [Header("연출")]
        [SerializeField] private bool applyUpImpulse = true;
        [Min(0f)] [SerializeField] private float upImpulse = 2.0f;

        public static event Action<int, Vector2, int> OnMerge;
        public static event Action<int, Vector2, int> OnMergeStatic;

        private void OnEnable()
        {
            if (merger == null) merger = FindFirstObjectByType<FruitMerger>();
            if (merger == null) { enabled = false; return; }
            merger.OnMergeResolved += HandleMergeResolved;
        }

        private void OnDisable()
        {
            if (merger != null) merger.OnMergeResolved -= HandleMergeResolved;
        }

        private void HandleMergeResolved(int mergedLevel, FruitData nextStage, Vector2 mid)
        {
            if (nextStage == null || nextStage.prefab == null) return;

            var go = Instantiate(nextStage.prefab, mid, Quaternion.identity, spawnParent);
            var fruit = go.GetComponent<Fruit>();
            if (fruit != null) fruit.Apply(nextStage, SuikaConstants.BaseFruitRadius);

            if (applyUpImpulse)
            {
                var rb = go.GetComponent<Rigidbody2D>();
                if (rb != null) rb.AddForce(Vector2.up * upImpulse, ForceMode2D.Impulse);
            }

            OnMerge?.Invoke(nextStage.level, mid, nextStage.mergeScore);
            OnMergeStatic?.Invoke(nextStage.level, mid, nextStage.mergeScore);
        }
    }
}