using GamePlay.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace UnityEngine.UI
{
    [CustomEditor(typeof(UILoopList))]
    public class UILoopListEditor : UnityEditor.Editor
    {
        private SerializedProperty _template;
        private SerializedProperty _content;
        private SerializedProperty _direction;
        private SerializedProperty _reverseDirection;
        private SerializedProperty _snapTime;
        private SerializedProperty _padding;
        private SerializedProperty _spacing;
        private SerializedProperty _scrollSensitivity;
        private SerializedProperty _decelerationRate;
        private SerializedProperty _elasticity;

        private void OnEnable()
        {
            _template = serializedObject.FindProperty("template");
            _content = serializedObject.FindProperty("content");
            _direction = serializedObject.FindProperty("direction");
            _reverseDirection = serializedObject.FindProperty("reverseDirection");
            _padding = serializedObject.FindProperty("padding");
            _spacing = serializedObject.FindProperty("spacing");
            _scrollSensitivity = serializedObject.FindProperty("scrollSensitivity");
            _snapTime = serializedObject.FindProperty("snapTime");
            _decelerationRate = serializedObject.FindProperty("decelerationRate");
            _elasticity = serializedObject.FindProperty("elasticity");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Template
            EditorGUILayout.PropertyField(_template, new GUIContent("Template", "元素预制体，可以通过Init方法动态传入"));
            
            // Content
            EditorGUILayout.PropertyField(_content, new GUIContent("Content", "元素容器，必须设置"));
            
            EditorGUILayout.Space(5);
            
            // Direction
            EditorGUILayout.PropertyField(_direction, new GUIContent("Direction", "滚动方向"));
            
            // Reverse Direction
            EditorGUILayout.PropertyField(_reverseDirection, new GUIContent("Reverse Direction", "是否反方向排布，自上向下与自右向左为正方向"));
            
            EditorGUILayout.Space(5);
            
            // Padding
            EditorGUILayout.PropertyField(_padding, new GUIContent("Padding", "内容边距"));
            
            // Spacing
            EditorGUILayout.PropertyField(_spacing, new GUIContent("Spacing", "元素间距"));
            
            EditorGUILayout.Space(5);
            
            // Scroll Sensitivity
            EditorGUILayout.PropertyField(_scrollSensitivity, new GUIContent("Scroll Sensitivity", "对滚轮和触控板滚动事件的敏感性，值越大越敏感"));
            
            EditorGUILayout.BeginHorizontal();
            var hasSnap = _snapTime.floatValue > 0f;
            var newHasSnap = EditorGUILayout.Toggle(hasSnap, GUILayout.Width(14));
            if (newHasSnap != hasSnap)
            {
                _snapTime.floatValue = newHasSnap ? 0.2f : 0f;
            }
            
            EditorGUI.BeginDisabledGroup(!newHasSnap);
            EditorGUILayout.PropertyField(_snapTime, new GUIContent("Snap Time", "磁吸模式滚动停止时会自动对齐到最近的元素位置的平滑时间"));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            var hasInertia = _decelerationRate.floatValue > 0f;
            var newHasInertia = EditorGUILayout.Toggle(hasInertia, GUILayout.Width(14));
            if (newHasInertia != hasInertia)
            {
                _decelerationRate.floatValue = newHasInertia ? 0.135f : 0f;
            }
            
            EditorGUI.BeginDisabledGroup(!newHasInertia);
            EditorGUILayout.PropertyField(_decelerationRate, new GUIContent("Inertia", "惯性衰减速度，值越大衰减越快，0为无惯性效果"));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            var hasElasticity = _elasticity.floatValue > 0f;
            var newHasElasticity = EditorGUILayout.Toggle(hasElasticity, GUILayout.Width(14));
            if (newHasElasticity != hasElasticity)
            {
                _elasticity.floatValue = newHasElasticity ? 0.1f : 0f;
            }
            
            EditorGUI.BeginDisabledGroup(!newHasElasticity);
            EditorGUILayout.PropertyField(_elasticity, new GUIContent("Elasticity", "回弹时间，值越小回弹越快，0为无弹性效果"));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        [MenuItem("GameObject/FrameworkUI/UILoopList", false, 5)]
        private static void CreateCustomLoopList()
        {
            // 创建根节点
            var loopList = new GameObject("@LoopList");
            Undo.RegisterCreatedObjectUndo(loopList, "Create Custom LoopList");

            // 设置父子关系
            var parent = Selection.activeGameObject;
            if (parent == null)
            {
                var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                if (prefabStage != null)
                {
                    parent = prefabStage.prefabContentsRoot;
                }
            }
            
            if (parent != null)
            {
                GameObjectUtility.SetParentAndAlign(loopList, parent);
            }

            // 添加RectTransform
            loopList.AddComponent<RectTransform>();
            // 添加UILoopList组件
            var loopListComponent = loopList.AddComponent<UILoopList>();
            UIModuleEditorUtils.ApplyPreset(loopListComponent, "UILoopList");
            
            // 添加RectMask2D
            loopList.AddComponent<RectMask2D>();
            
            // 添加Empty4Raycast
            loopList.AddComponent<Empty4Raycast>();

            // 创建Content子节点
            var content = new GameObject("Content");
            Undo.RegisterCreatedObjectUndo(content, "Create Content");
            GameObjectUtility.SetParentAndAlign(content, loopList);
            
            var contentRect = content.AddComponent<RectTransform>();

            // 将Content设置为UILoopList的content属性
            var serializedObject = new SerializedObject(loopListComponent);
            var contentProperty = serializedObject.FindProperty("content");
            contentProperty.objectReferenceValue = contentRect;
            serializedObject.ApplyModifiedProperties();

            // 选中创建的对象
            Selection.activeGameObject = loopList;

            // 标记场景为已修改
            EditorSceneManager.MarkSceneDirty(loopList.scene);
        }
    }
}

