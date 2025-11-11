using GamePlay.Editor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UnityEditor.UI
{
    [CustomEditor(typeof(UIToggle))]
    public class UIToggleEditor : SelectableEditor
    {
        private UIToggle _target;
        private SerializedProperty _clickSound;
        private SerializedProperty _clickDisableSound;
        private SerializedProperty _clickCooldownTime;
        private SerializedProperty _showDetailSetting;
        private SerializedProperty _toggleGroup;
        private SerializedProperty _checkMark;
        private SerializedProperty _isOn;
        private SerializedProperty _text;

        protected override void OnEnable()
        {
            base.OnEnable();

            _target = target as UIToggle;
            _clickSound = serializedObject.FindProperty("clickSound");
            _clickDisableSound = serializedObject.FindProperty("clickDisableSound");
            _clickCooldownTime = serializedObject.FindProperty("clickCooldownTime");
            _showDetailSetting = serializedObject.FindProperty("showDetailSetting");
            _isOn = serializedObject.FindProperty("isOn");
            _toggleGroup = serializedObject.FindProperty("toggleGroup");
            _checkMark = serializedObject.FindProperty("checkMark");
            _text = serializedObject.FindProperty("text");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            base.OnInspectorGUI();
            
            var isOn = EditorGUILayout.Toggle("IsOn", _isOn.boolValue);
            if (isOn != _isOn.boolValue)
            {
                if (_target.toggleGroup != null)
                {
                    _target.toggleGroup.SetToggle(_target, isOn);
                }
                else
                {
                    _target.SetToggle(isOn);
                }
            }
            
            EditorGUILayout.PropertyField(_text);
            EditorGUILayout.PropertyField(_checkMark, new GUIContent("Check Mark", "开关时显隐的节点"));
            EditorGUILayout.PropertyField(_toggleGroup, new GUIContent("Toggle Group", "所属的Toggle组"));
            
            _showDetailSetting.boolValue = EditorGUILayout.Foldout(_showDetailSetting.boolValue, "Other Settings");
            if (_showDetailSetting.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_clickSound, new GUIContent("Click Sound", "生效时点击音效"));
                EditorGUILayout.PropertyField(_clickDisableSound, new GUIContent("Disable Sound", "屏蔽时点击音效"));
                EditorGUILayout.PropertyField(_clickCooldownTime, new GUIContent("Click Cooldown", "有效点击的间隔时间"));
                EditorGUI.indentLevel--;
            }
            
            serializedObject.ApplyModifiedProperties();
        }

        [MenuItem("GameObject/FrameworkUI/UIToggle-TMP", false, 6)]
        private static void CreateCustomToggle()
        {
            // Create the core object
            var toggle = new GameObject("@Toggle");
            Undo.RegisterCreatedObjectUndo(toggle, "Create Custom Toggle");

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
            GameObjectUtility.SetParentAndAlign(toggle, parent);

            // Add necessary components
            var rectTransform = toggle.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = Vector2.zero;
            UIEditorUtils.ApplyPreset(toggle.AddComponent<UIToggle>(), "UIToggle");

            // Create Background
            var background = new GameObject("Background");
            Undo.RegisterCreatedObjectUndo(background, "Create Background");
            var backgroundRectTransform = background.AddComponent<RectTransform>();
            backgroundRectTransform.SetParent(toggle.transform, false);
            var backgroundImage = background.AddComponent<Image>();
            backgroundImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            backgroundImage.type = Image.Type.Sliced;

            // Create Checkmark
            var checkmark = new GameObject("Checkmark");
            Undo.RegisterCreatedObjectUndo(checkmark, "Create Checkmark");
            var checkmarkRectTransform = checkmark.AddComponent<RectTransform>();
            checkmarkRectTransform.SetParent(background.transform, false);
            var canvasGroup = checkmark.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            var checkmarkImage = checkmark.AddComponent<Image>();
            checkmarkImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");

            // Create TMP text
            var textObj = new GameObject("Text (TMP)");
            Undo.RegisterCreatedObjectUndo(textObj, "Create TMP Text");
            var textRectTransform = textObj.AddComponent<RectTransform>();
            textRectTransform.SetParent(toggle.transform, false);
            var textMeshPro = textObj.AddComponent<TextMeshProUGUI>();
            UIEditorUtils.ApplyPreset(textMeshPro, "TextMeshProUGUI");
            
            // Assign Background and Checkmark to UIToggle
            var uiToggle = toggle.GetComponent<UIToggle>();
            uiToggle.targetGraphic = backgroundImage;
            uiToggle.checkMark = canvasGroup;
            uiToggle.text = textMeshPro;

            // Mark scene as modified
            EditorSceneManager.MarkSceneDirty(toggle.scene);
        }
    }
}