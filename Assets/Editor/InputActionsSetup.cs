#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SuikaGame.EditorTools
{
    /// <summary>
    /// InputActions.inputactions 의 C# 래퍼(InputActions.cs) 자동 생성 옵션을 켜는 메뉴.
    /// Importer 의 m_GenerateWrapperCode 필드를 SerializedObject 로 안전하게 설정한다.
    /// 참고: 이슈 #3.
    /// </summary>
    public static class InputActionsSetup
    {
        public const string InputActionsAssetPath = "Assets/Settings/Input/InputActions.inputactions";
        public const string WrapperClassName = "InputActions";
        public const string WrapperNamespace = "SuikaGame.Input";

        [MenuItem("Tools/Suika/Enable Input Actions Wrapper Code")]
        public static void EnableWrapperCode()
        {
            var importer = AssetImporter.GetAtPath(InputActionsAssetPath);
            if (importer == null)
            {
                Debug.LogError($"[Suika] InputActions 자산을 찾을 수 없음: {InputActionsAssetPath}");
                return;
            }

            var so = new SerializedObject(importer);

            var generateProp = so.FindProperty("m_GenerateWrapperCode");
            var classProp = so.FindProperty("m_WrapperClassName");
            var nsProp = so.FindProperty("m_WrapperCodeNamespace");

            if (generateProp == null)
            {
                Debug.LogError("[Suika] m_GenerateWrapperCode 필드를 찾지 못함. InputSystem 패키지 버전 확인 필요.");
                return;
            }

            generateProp.boolValue = true;
            if (classProp != null) classProp.stringValue = WrapperClassName;
            if (nsProp != null) nsProp.stringValue = WrapperNamespace;

            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.ImportAsset(InputActionsAssetPath, ImportAssetOptions.ForceUpdate);
            Debug.Log($"[Suika] {InputActionsAssetPath} → wrapper code 생성 활성화 (class={WrapperClassName}, ns={WrapperNamespace})");
        }
    }
}
#endif
