#if UNITY_EDITOR
using System.IO;
using SuikaGame.Data;
using UnityEditor;
using UnityEngine;

namespace SuikaGame.EditorTools
{
    /// <summary>
    /// GDD §3 표 기반으로 11종 FruitData + DropPoolData(1~5번) 에셋을 일괄 생성.
    /// Unity 메뉴: Tools/Suika/Generate Fruit Data Assets
    /// 동일 이름 에셋이 이미 있으면 필드만 갱신해서 덮어쓴다.
    /// </summary>
    public static class FruitDataGenerator
    {
        private const string FruitFolder = "Assets/Data/Fruits";

        private struct Row
        {
            public int level;
            public string nameKo;
            public string nameEn;
            public float relativeRadius;
            public int mergeScore;

            public Row(int l, string ko, string en, float r, int s)
            {
                level = l; nameKo = ko; nameEn = en; relativeRadius = r; mergeScore = s;
            }
        }

        // GDD §3 표
        private static readonly Row[] Rows = new[]
        {
            new Row( 1, "체리",     "Cherry",      1.0f,  1),
            new Row( 2, "딸기",     "Strawberry",  1.5f,  3),
            new Row( 3, "포도",     "Grapes",      2.0f,  6),
            new Row( 4, "데코폰",   "Dekopon",     2.5f, 10),
            new Row( 5, "감",       "Persimmon",   3.0f, 15),
            new Row( 6, "사과",     "Apple",       3.7f, 21),
            new Row( 7, "배",       "Pear",        4.4f, 28),
            new Row( 8, "복숭아",   "Peach",       5.2f, 36),
            new Row( 9, "파인애플", "Pineapple",   5.9f, 45),
            new Row(10, "멜론",     "Melon",       7.4f, 55),
            new Row(11, "수박",     "Watermelon",  8.8f, 66),
        };

        [MenuItem("Tools/Suika/Generate Fruit Data Assets")]
        public static void Generate()
        {
            EnsureFolder(FruitFolder);

            var created = new FruitData[Rows.Length];

            // 1단계: 11종 FruitData 자산을 생성/갱신
            for (int i = 0; i < Rows.Length; i++)
            {
                var row = Rows[i];
                string fileName = $"{row.level:00}_{row.nameEn}.asset";
                string path = $"{FruitFolder}/{fileName}";

                var data = AssetDatabase.LoadAssetAtPath<FruitData>(path);
                bool isNew = data == null;
                if (isNew)
                {
                    data = ScriptableObject.CreateInstance<FruitData>();
                    AssetDatabase.CreateAsset(data, path);
                }

                data.level = row.level;
                data.nameKo = row.nameKo;
                data.nameEn = row.nameEn;
                data.relativeRadius = row.relativeRadius;
                data.mergeScore = row.mergeScore;
                // icon 은 사용자가 직접 할당 (아트 미정)

                created[i] = data;
                EditorUtility.SetDirty(data);
            }

            // 2단계: nextStage 연결 (마지막은 null 유지)
            for (int i = 0; i < created.Length; i++)
            {
                created[i].nextStage = (i < created.Length - 1) ? created[i + 1] : null;
                EditorUtility.SetDirty(created[i]);
            }

            // 3단계: DropPoolData (1~5번)
            string poolPath = $"{FruitFolder}/DropPool_1to5.asset";
            var pool = AssetDatabase.LoadAssetAtPath<DropPoolData>(poolPath);
            if (pool == null)
            {
                pool = ScriptableObject.CreateInstance<DropPoolData>();
                AssetDatabase.CreateAsset(pool, poolPath);
            }
            pool.entries.Clear();
            for (int i = 0; i < 5; i++)
            {
                pool.entries.Add(new DropPoolData.Entry { fruit = created[i], weight = 1f });
            }
            EditorUtility.SetDirty(pool);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Suika] FruitData 11종 + DropPool 생성/갱신 완료: {FruitFolder}");
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = pool;
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath)) return;

            string parent = Path.GetDirectoryName(assetPath).Replace('\\', '/');
            string leaf = Path.GetFileName(assetPath);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
