using GamePlay.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace UnityEngine.UI
{
    [CustomEditor(typeof(UIDrag))]
    public class UIDragEditor : Editor
    {
        private SerializedProperty _interactable;
        private SerializedProperty _directionType;
        private SerializedProperty _maxRange;
        private SerializedProperty _angleThreshold;
        private SerializedProperty _dragDelay;
        private SerializedProperty _dragTarget;

        private void OnEnable() {
            _interactable = serializedObject.FindProperty("interactable");
            _directionType = serializedObject.FindProperty("directionType");
            _maxRange = serializedObject.FindProperty("maxRange");
            _angleThreshold = serializedObject.FindProperty("angleThreshold");
            _dragDelay = serializedObject.FindProperty("dragDelay");
            _dragTarget = serializedObject.FindProperty("dragTarget");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_interactable, new GUIContent("Interactable", "是否可交互"));
            EditorGUILayout.PropertyField(_directionType, new GUIContent("Direction", "允许拖拽的方向"));
            EditorGUILayout.PropertyField(_maxRange, new GUIContent("DragRange", "拖拽区域限制,0表示无限制"));
            if (_directionType.enumValueIndex != 0) {
                EditorGUILayout.PropertyField(_angleThreshold, new GUIContent("BeginDragAngle", "起始拖拽方向与主方向的最大允许偏差角度"));
            }
            EditorGUILayout.PropertyField(_dragDelay, new GUIContent("BeginDelay", "按住超过设置时间后允许开始拖拽"));
            EditorGUILayout.PropertyField(_dragTarget, new GUIContent("DragTarget", "拖拽时移动的目标对象"));
            if (_dragTarget.objectReferenceValue != null) {
                var dragTargetTransform = ((Transform)_dragTarget.objectReferenceValue);
                var dragTransform = ((UIDrag)target).transform;
                if (dragTargetTransform.parent != dragTransform) {
                    _dragTarget.objectReferenceValue = null;
                    Debug.LogError($"DragTarget必须是当前节点的一级子节点");
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            var drag = (UIDrag)target;
            if (drag == null || drag.maxRange == Vector2.zero) return;

            Handles.color = Color.green;
            var position = drag.transform.position;
            var size = new Vector3(drag.maxRange.x, drag.maxRange.y, 0);

            switch (drag.directionType)
            {
                case UIDrag.DragDirection.Up:
                    Handles.DrawWireCube(position + new Vector3(0, size.y / 2, 0), size);
                    DrawAngleArc(position, 90, drag.angleThreshold, size.y);
                    break;
                case UIDrag.DragDirection.Down:
                    Handles.DrawWireCube(position - new Vector3(0, size.y / 2, 0), size);
                    DrawAngleArc(position, 270, drag.angleThreshold, size.y);
                    break;
                case UIDrag.DragDirection.Left:
                    Handles.DrawWireCube(position - new Vector3(size.x / 2, 0, 0), size);
                    DrawAngleArc(position, 180, drag.angleThreshold, size.x);
                    break;
                case UIDrag.DragDirection.Right:
                    Handles.DrawWireCube(position + new Vector3(size.x / 2, 0, 0), size);
                    DrawAngleArc(position, 0, drag.angleThreshold, size.x);
                    break;
                case UIDrag.DragDirection.None:
                    Handles.DrawWireCube(position, size);
                    break;
            }
        }

        private void DrawAngleArc(Vector3 position, float angle, float angleThreshold, float radius)
        {
            Handles.color = Color.red;
            var startDirection = Quaternion.Euler(0, 0, angle - angleThreshold) * Vector3.right;
            var endDirection = Quaternion.Euler(0, 0, angle + angleThreshold) * Vector3.right;
            Handles.DrawWireArc(position, Vector3.forward, startDirection, angleThreshold * 2, radius);
            Handles.DrawLine(position, position + startDirection * radius);
            Handles.DrawLine(position, position + endDirection * radius);
        }
        
        [MenuItem("GameObject/FrameworkUI/UIDrag", false, 5)]
        private static void CreateCustomButton()
        {
            // 创建核心对象
            var drag = new GameObject("@Drag");
            Undo.RegisterCreatedObjectUndo(drag, "Create Custom Drag");

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
            GameObjectUtility.SetParentAndAlign(drag, parent);

            // 添加必要组件
            var rectTransform = drag.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = Vector2.zero;
            drag.AddComponent<Empty4Raycast>();
            UIEditorUtils.ApplyPreset(drag.AddComponent<UIDrag>(), "UIDrag");

            // 标记场景为已修改
            EditorSceneManager.MarkSceneDirty(drag.scene);
        }
    }
}