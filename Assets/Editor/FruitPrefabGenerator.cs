#if UNITY_EDITOR
using System.IO;
using SuikaGame.Data;
using SuikaGame.Gameplay;
using UnityEditor;
using UnityEngine;

namespace SuikaGame.EditorTools
{
    /// <summary>
    /// FruitData 11종을 기반으로 동일 구조의 과일 프리팹 11개를 생성/갱신.
    /// 참고: GDD §2-2, §3. 이슈 #8.
    /// Unity 메뉴: Tools/Suika/Generate Fruit Prefabs
    /// </summary>
    public static class FruitPrefabGenerator
    {
        private const string FruitDataFolder = "Assets/Data/Fruits";
        private const string FruitPrefabFolder = "Assets/Prefabs/Fruits";
        private const string MaterialFolder = "Assets/Materials";
        private const string FruitMaterialPath = MaterialFolder + "/SuikaFruit.physicsMaterial2D";

        // ---------- 과일 물리 머티리얼 ----------

        [MenuItem("Tools/Suika/Create Fruit Physics Material")]
        public static PhysicsMaterial2D CreateOrUpdateFruitMaterial()
        {
            EnsureFolder(MaterialFolder);

            var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(FruitMaterialPath);
            if (mat == null)
            {
                mat = new PhysicsMaterial2D("SuikaFruit");
                AssetDatabase.CreateAsset(mat, FruitMaterialPath);
            }
            // 통통 튀게는 하되 과하지 않도록.
            mat.friction = 0.2f;
            mat.bounciness = 0.2f;

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Suika] SuikaFruit.physicsMaterial2D 준비 (friction=0.2, bounciness=0.2)");
            return mat;
        }

        // ---------- 11종 프리팹 생성 ----------

        [MenuItem("Tools/Suika/Generate Fruit Prefabs")]
        public static void GenerateAll()
        {
            // 사전 준비: 레이어 + 머티리얼
            SuikaSetupTools.SetupLayers();
            int fruitLayer = LayerMask.NameToLayer(SuikaSetupTools.FruitLayerName);
            var fruitMat = CreateOrUpdateFruitMaterial();

            EnsureFolder(FruitPrefabFolder);

            // 11개 FruitData 로드
            var dataList = new FruitData[11];
            for (int level = 1; level <= 11; level++)
            {
                var fd = LoadFruitDataByLevel(level);
                if (fd == null)
                {
                    Debug.LogError($"[Suika] FruitData 로드 실패: level {level}. 먼저 Tools/Suika/Generate Fruit Data Assets 실행 필요.");
                    return;
                }
                dataList[level - 1] = fd;
            }

            for (int i = 0; i < dataList.Length; i++)
            {
                CreateOrUpdatePrefab(dataList[i], fruitLayer, fruitMat);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Suika] 11종 과일 프리팹 생성/갱신 완료.");
        }

        private static void CreateOrUpdatePrefab(FruitData data, int fruitLayer, PhysicsMaterial2D fruitMat)
        {
            string path = $"{FruitPrefabFolder}/{data.level:00}_{data.nameEn}.prefab";
            GameObject root;
            bool isNew;

            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                // 기존 프리팹을 직접 수정하기 위해 contents 로 로드
                root = PrefabUtility.LoadPrefabContents(path);
                isNew = false;
            }
            else
            {
                root = new GameObject();
                isNew = true;
            }

            // 컴포넌트 보장
            var sr = root.GetComponent<SpriteRenderer>();
            if (sr == null) sr = root.AddComponent<SpriteRenderer>();

            var col = root.GetComponent<CircleCollider2D>();
            if (col == null) col = root.AddComponent<CircleCollider2D>();
            col.sharedMaterial = fruitMat;

            var rb = root.GetComponent<Rigidbody2D>();
            if (rb == null) rb = root.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1f;
            rb.freezeRotation = false; // 회전 허용
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var fruit = root.GetComponent<Fruit>();
            if (fruit == null) fruit = root.AddComponent<Fruit>();

            // 데이터·반지름·스프라이트 적용
            fruit.Apply(data, SuikaConstants.BaseFruitRadius);

            // 레이어 (Fruit)
            if (fruitLayer >= 0) root.layer = fruitLayer;

            // 저장
            if (isNew)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Object.DestroyImmediate(root);
            }
            else
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }

            // FruitData 에 프리팹 역참조 백필 (#11 스폰에서 사용)
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabAsset != null && data.prefab != prefabAsset)
            {
                data.prefab = prefabAsset;
                EditorUtility.SetDirty(data);
            }
        }

        private static FruitData LoadFruitDataByLevel(int level)
        {
            // 이름 규약(01_Cherry.asset 등)에 의존하지 않고 GUID 기반 스캔
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(FruitData)}", new[] { FruitDataFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                string p = AssetDatabase.GUIDToAssetPath(guids[i]);
                var fd = AssetDatabase.LoadAssetAtPath<FruitData>(p);
                if (fd != null && fd.level == level) return fd;
            }
            return null;
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath)) return;
            string parent = Path.GetDirectoryName(assetPath).Replace('\\', '/');
            string leaf = Path.GetFileName(assetPath);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
