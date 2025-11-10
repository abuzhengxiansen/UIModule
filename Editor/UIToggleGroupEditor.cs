using UnityEditor;
using UnityEditor.SceneManagement;

namespace UnityEngine.UI
{
    [CustomEditor(typeof(UIToggleGroup))]
    public class UIToggleGroupEditor : Editor
    {
        private UIToggleGroup _target;
        private SerializedProperty _templateToggle;
        private SerializedProperty _defaultToggle;
        private SerializedProperty _content;
        private SerializedProperty _allowAllOff;
        private SerializedProperty _isDynamic;

        private void OnEnable()
        {
            _target = (UIToggleGroup)target;
            _templateToggle = serializedObject.FindProperty("templateToggle");
            _defaultToggle = serializedObject.FindProperty("defaultToggle");
            _content = serializedObject.FindProperty("content");
            _allowAllOff = serializedObject.FindProperty("allowAllOff");
            _isDynamic = serializedObject.FindProperty("isDynamic");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.PropertyField(_isDynamic, new GUIContent("Is Dynamic", "是否动态生成成员Toggle"));
            if (_isDynamic.boolValue)
            {
                EditorGUILayout.PropertyField(_templateToggle, new GUIContent("Template Toggle", "成员toggle模板"));
                EditorGUILayout.PropertyField(_content, new GUIContent("Content", "成员toggle父节点"));
            }
            
            EditorGUILayout.PropertyField(_allowAllOff, new GUIContent("Allow All Off", "是否允许所有Toggle都关闭"));
            if (!_allowAllOff.boolValue)
            {
                EditorGUILayout.PropertyField(_defaultToggle);
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        [MenuItem("GameObject/FrameworkUI/UIToggleGroup", false, 7)]
        private static void CreateCustomToggleGroup()
        {
            // 创建核心对象
            var toggleGroup = new GameObject("@ToggleGroup");
            Undo.RegisterCreatedObjectUndo(toggleGroup, "Create Custom Toggle Group");

            // 设置父子关系
            var parent = Selection.activeGameObject;
            if (parent == null)
            {
                var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                if (prefabStage == null)
                {
                    Debug.LogWarning("No GameObject selected and no prefab stage open. Please select a GameObject or open a prefab stage.");
                    return;
                }
                parent = prefabStage.prefabContentsRoot;
            }
            GameObjectUtility.SetParentAndAlign(toggleGroup, parent);

            // 添加必要组件
            var rectTransform = toggleGroup.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = Vector2.zero;
            toggleGroup.AddComponent<UIToggleGroup>();

            // 标记场景为已修改
            EditorSceneManager.MarkSceneDirty(toggleGroup.scene);
        }
    }
}