#if UNITY_EDITOR
using System.IO;
using SuikaGame.Gameplay;
using UnityEditor;
using UnityEngine;

namespace SuikaGame.EditorTools
{
    /// <summary>
    /// 박스 컨테이너 / 물리 머티리얼 / 레이어 일괄 세팅용 Editor 메뉴.
    /// 참고: GDD §2-2, §7. 이슈 #5.
    /// </summary>
    public static class SuikaSetupTools
    {
        private const string MaterialFolder = "Assets/Materials";
        private const string WallMaterialPath = MaterialFolder + "/SuikaWall.physicsMaterial2D";

        public const string FruitLayerName = "Fruit";
        public const string WallLayerName = "Wall";

        // ---------- 1) 레이어 세팅 ----------

        [MenuItem("Tools/Suika/Setup Physics Layers")]
        public static void SetupLayers()
        {
            int fruitIdx = EnsureLayer(FruitLayerName);
            int wallIdx = EnsureLayer(WallLayerName);
            Debug.Log($"[Suika] 레이어 확보: Fruit={fruitIdx}, Wall={wallIdx}");
        }

        /// <summary>
        /// TagManager.asset 의 빈 user layer 슬롯(8~31)에 이름을 채워넣고 인덱스를 돌려준다.
        /// 이미 존재하면 그 인덱스를 그대로 반환.
        /// </summary>
        public static int EnsureLayer(string layerName)
        {
            var tagManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManager == null || tagManager.Length == 0)
            {
                Debug.LogError("[Suika] TagManager.asset 로드 실패");
                return -1;
            }

            var so = new SerializedObject(tagManager[0]);
            var layers = so.FindProperty("layers");
            if (layers == null || !layers.isArray)
            {
                Debug.LogError("[Suika] TagManager 'layers' 속성을 찾을 수 없음");
                return -1;
            }

            // 이미 있나?
            for (int i = 0; i < layers.arraySize; i++)
            {
                var sp = layers.GetArrayElementAtIndex(i);
                if (sp.stringValue == layerName) return i;
            }

            // 8번부터 user layer
            for (int i = 8; i < layers.arraySize; i++)
            {
                var sp = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(sp.stringValue))
                {
                    sp.stringValue = layerName;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    return i;
                }
            }
            Debug.LogError($"[Suika] '{layerName}' 추가 실패 - 빈 user layer 슬롯 없음");
            return -1;
        }

        // ---------- 2) 벽 물리 머티리얼 ----------

        [MenuItem("Tools/Suika/Create Wall Physics Material")]
        public static PhysicsMaterial2D CreateOrUpdateWallMaterial()
        {
            EnsureFolder(MaterialFolder);

            var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(WallMaterialPath);
            if (mat == null)
            {
                mat = new PhysicsMaterial2D("SuikaWall");
                AssetDatabase.CreateAsset(mat, WallMaterialPath);
            }
            mat.friction = 0.4f;
            mat.bounciness = 0.1f;

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Suika] PhysicsMaterial2D 준비: {WallMaterialPath} (friction=0.4, bounciness=0.1)");
            return mat;
        }

        // ---------- 3) 씬에 박스 생성 ----------

        [MenuItem("Tools/Suika/Create Box Container In Scene")]
        public static void CreateBoxContainerInScene()
        {
            // 사전 준비: 레이어 + 머티리얼
            SetupLayers();
            var mat = CreateOrUpdateWallMaterial();

            int wallLayer = LayerMask.NameToLayer(WallLayerName);

            // 기존 BoxContainer가 있으면 그걸 재사용
            var existing = Object.FindFirstObjectByType<BoxContainer>();
            BoxContainer box;
            if (existing != null)
            {
                box = existing;
                Debug.Log("[Suika] 기존 BoxContainer 재사용");
            }
            else
            {
                var go = new GameObject("BoxContainer");
                box = go.AddComponent<BoxContainer>();
                Undo.RegisterCreatedObjectUndo(go, "Create BoxContainer");
            }

            box.wallMaterial = mat;
            box.EnsureChildren();
            box.Arrange();

            // 자식에 레이어 적용
            if (wallLayer >= 0)
            {
                ApplyLayerRecursive(box.gameObject, wallLayer);
            }

            EditorUtility.SetDirty(box);
            Selection.activeGameObject = box.gameObject;
            Debug.Log("[Suika] BoxContainer 씬에 배치 완료. 인스펙터에서 크기 튜닝 가능.");
        }

        private static void ApplyLayerRecursive(GameObject root, int layer)
        {
            // 루트는 일반 GameObject로 두고 자식(콜라이더) 에만 Wall 레이어 적용
            foreach (Transform t in root.transform)
            {
                t.gameObject.layer = layer;
            }
        }

        // ---------- 4) 데스라인 ----------

        [MenuItem("Tools/Suika/Create Death Line In Scene")]
        public static void CreateDeathLineInScene()
        {
            var box = Object.FindFirstObjectByType<BoxContainer>();
            if (box == null)
            {
                Debug.LogError("[Suika] 씬에 BoxContainer 가 없음. 먼저 Tools/Suika/Create Box Container In Scene 실행.");
                return;
            }

            var existing = Object.FindFirstObjectByType<DeathLine>();
            DeathLine dl;
            if (existing != null)
            {
                dl = existing;
            }
            else
            {
                var go = new GameObject("DeathLine");
                go.transform.SetParent(box.transform.parent, false);
                go.transform.position = box.transform.position;
                dl = go.AddComponent<DeathLine>();
                Undo.RegisterCreatedObjectUndo(go, "Create DeathLine");
            }

            // SerializedObject 로 box 필드 설정 (private SerializeField)
            var so = new SerializedObject(dl);
            so.FindProperty("box").objectReferenceValue = box;
            so.ApplyModifiedPropertiesWithoutUndo();

            dl.EnsureChildren();
            dl.Arrange();

            Selection.activeGameObject = dl.gameObject;
            Debug.Log("[Suika] DeathLine 씬에 배치 완료.");
        }

        // ---------- 5) 한 번에 ----------

        [MenuItem("Tools/Suika/Run Full Box Setup")]
        public static void RunFullSetup()
        {
            SetupLayers();
            CreateOrUpdateWallMaterial();
            CreateBoxContainerInScene();
            CreateDeathLineInScene();
        }

        // ---------- helpers ----------

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
