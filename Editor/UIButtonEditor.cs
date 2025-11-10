using GamePlay.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace UnityEngine.UI
{
    [CustomEditor(typeof(UIButton))]
    public class UIButtonEditor : UnityEditor.UI.ButtonEditor
    {
        private UIButton _target;
        private SerializedProperty _clickSound;
        private SerializedProperty _clickDisableSound;
        private SerializedProperty _clickCooldownTime;
        private SerializedProperty _doubleClickEffectTime;
        private SerializedProperty _pressEffectInterval;
        private SerializedProperty _pressEffectTime;
        private SerializedProperty _clickMode;
        private SerializedProperty _clickEffectMode;
        private SerializedProperty _isOpenPress;
        private SerializedProperty _showDetailSetting;

        protected override void OnEnable()
        {
            base.OnEnable();

            _target = target as UIButton;
            _clickSound = serializedObject.FindProperty("clickSound");
            _clickDisableSound = serializedObject.FindProperty("clickDisableSound");
            _clickCooldownTime = serializedObject.FindProperty("clickCooldownTime");
            _doubleClickEffectTime = serializedObject.FindProperty("doubleClickEffectTime");
            _pressEffectInterval = serializedObject.FindProperty("pressEffectInterval");
            _pressEffectTime = serializedObject.FindProperty("pressEffectTime");
            _clickMode = serializedObject.FindProperty("clickMode");
            _clickEffectMode = serializedObject.FindProperty("clickEffectMode");
            _isOpenPress = serializedObject.FindProperty("isOpenPress");
            _showDetailSetting = serializedObject.FindProperty("showDetailSetting");

            if (_clickEffectMode.enumValueIndex != 0 && _target.GetComponent<ClickEffectSingle>() == null)
            {
                ApplyClickEffectMode();
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            base.OnInspectorGUI();

            _clickMode.enumValueIndex =
                EditorGUILayout.Popup("Click Mode", _clickMode.enumValueIndex, _clickMode.enumDisplayNames);
            var clickEffectMode = EditorGUILayout.Popup("Click Effect Mode", _clickEffectMode.enumValueIndex,
                _clickEffectMode.enumDisplayNames);
            if (clickEffectMode != _clickEffectMode.enumValueIndex)
            {
                _clickEffectMode.enumValueIndex = clickEffectMode;
                ApplyClickEffectMode();
            }

            EditorGUILayout.PropertyField(_isOpenPress, new GUIContent("Is Open Press", "是否开启按压效果"));

            _showDetailSetting.boolValue = EditorGUILayout.Foldout(_showDetailSetting.boolValue, "Other Settings");
            if (_showDetailSetting.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_clickSound, new GUIContent("Click Sound", "生效时点击音效"));
                EditorGUILayout.PropertyField(_clickDisableSound, new GUIContent("Disable Sound", "屏蔽时点击音效"));
                EditorGUILayout.PropertyField(_clickCooldownTime, new GUIContent("Click Cooldown", "有效点击的间隔时间"));
                
                if (_clickMode.enumValueIndex == (int)UIButton.ClickModes.Double)
                {
                    EditorGUILayout.PropertyField(_doubleClickEffectTime, new GUIContent("Double Click Effect Time", "有效双击的间隔时间"));
                }
                if (_isOpenPress.boolValue)
                {
                    EditorGUILayout.PropertyField(_pressEffectTime, new GUIContent("Press Effect Time", "按压起效的时间"));
                    EditorGUILayout.PropertyField(_pressEffectInterval, new GUIContent("Press Effect Interval", "按压循环触发的间隔时间"));
                }

                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void ApplyClickEffectMode()
        {
            DestroyImmediate(_target.GetComponent(typeof(ClickEffectSingle)));

            var effectComp = (ClickEffectModes)_clickEffectMode.enumValueIndex switch
            {
                ClickEffectModes.Jelly => _target.gameObject.AddComponent<ClickEffectJelly>(),
                _ => null
            };
            effectComp?.SetSelectable(_target);
        }
        
        [MenuItem("GameObject/FrameworkUI/UIButton-TMP", false, 4)]
        private static void CreateCustomButton()
        {
            // 创建核心对象
            var btn = new GameObject("@Btn");
            Undo.RegisterCreatedObjectUndo(btn, "Create Custom Button");

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
            GameObjectUtility.SetParentAndAlign(btn, parent);

            // 添加必要组件
            var rectTransform = btn.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = Vector2.zero;
            var image = btn.AddComponent<Image>();
            UIEditorUtils.ApplyPreset(btn.AddComponent<UIButton>(), "UIButton");

            // 创建TMP文本
            var textObj = new GameObject("Text (TMP)");
            Undo.RegisterCreatedObjectUndo(textObj, "Create TMP Text");
            var textRectTransform = textObj.AddComponent<RectTransform>();
            UIEditorUtils.ApplyPreset(textObj.AddComponent<TMPro.TextMeshProUGUI>(), "TextMeshProUGUI");
            GameObjectUtility.SetParentAndAlign(textObj, btn);

            // 标记场景为已修改
            EditorSceneManager.MarkSceneDirty(btn.scene);
        }
    }
}