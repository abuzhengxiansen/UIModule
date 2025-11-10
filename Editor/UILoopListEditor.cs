using GamePlay.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace UnityEngine.UI
{
    [CustomEditor(typeof(UILoopList))]
    public class UILoopListEditor : UnityEditor.UI.ScrollRectEditor
    {
        private UILoopList _Target;
        private SerializedProperty _ItemSize;
        private SerializedProperty _Direction;
        private SerializedProperty _ReverseArrangement;
        private SerializedProperty _IsDynamicSize;
        private SerializedProperty _Padding;
        private SerializedProperty _Spacing;

        protected override void OnEnable()
        {
            base.OnEnable();

            _Target = target as UILoopList;
            _ItemSize = serializedObject.FindProperty("itemSize");
            _Direction = serializedObject.FindProperty("direction");
            _ReverseArrangement = serializedObject.FindProperty("reverseArrangement");
            _IsDynamicSize = serializedObject.FindProperty("isDynamicSize");
            _Padding = serializedObject.FindProperty("padding");
            _Spacing = serializedObject.FindProperty("spacing");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            base.OnInspectorGUI();

            EditorGUILayout.PropertyField(_ItemSize, new GUIContent("TemplateCell"));
            EditorGUILayout.PropertyField(_Direction, new GUIContent("Direction"));
            EditorGUILayout.PropertyField(_ReverseArrangement, new GUIContent("Reverse Arrangement"));
            EditorGUILayout.PropertyField(_IsDynamicSize, new GUIContent("Dynamic Size"));
            EditorGUILayout.PropertyField(_Padding, new GUIContent("Padding"));
            EditorGUILayout.PropertyField(_Spacing, new GUIContent("Spacing"));

            serializedObject.ApplyModifiedProperties();
        }

        [MenuItem("GameObject/FrameworkUI/UILoopList", false, 8)]
        private static void CreateCustomLoopList()
        {
            // Create the core object
            var loopList = new GameObject("@UILoopList");
            Undo.RegisterCreatedObjectUndo(loopList, "Create Custom LoopList");

            // Set parent-child relationship
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
            GameObjectUtility.SetParentAndAlign(loopList, parent);

            // Add necessary components
            loopList.AddComponent<RectTransform>();
            var list = loopList.AddComponent<UILoopList>();
            UIEditorUtils.ApplyPreset(list, "UILoopList");
            
            var image = loopList.AddComponent<Empty4Raycast>();
            var mask = loopList.AddComponent<RectMask2D>();
            
            // Add Content object
            var content = new GameObject("Content");
            Undo.RegisterCreatedObjectUndo(content, "Create Content");
            GameObjectUtility.SetParentAndAlign(content, loopList);
            var contentRect = content.AddComponent<RectTransform>();
            list.content = contentRect;

            // Mark scene as modified
            EditorSceneManager.MarkSceneDirty(loopList.scene);
        }
    }
}