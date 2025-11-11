using GamePlay.Editor;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UI;

namespace UnityEngine.UI
{
    [CustomEditor(typeof(UISlider))]
    public class UISliderEditor : SelectableEditor
    {
        private UISlider _slider;
        private SerializedProperty _fill;
        private SerializedProperty _valueText;
        private SerializedProperty _handle;
        private SerializedProperty _valueFormat;
        private SerializedProperty _minValue;
        private SerializedProperty _maxValue;
        private SerializedProperty _currentValue;
        private SerializedProperty _isWholeValue;
        private SerializedProperty _showDetailSetting;
    
        protected override void OnEnable()
        {
            base.OnEnable();
            
            _slider = target as UISlider;
            _fill = serializedObject.FindProperty("fill");
            _valueText = serializedObject.FindProperty("valueText");
            _handle = serializedObject.FindProperty("handle");
            _valueFormat = serializedObject.FindProperty("valueFormat");
            _minValue = serializedObject.FindProperty("minValue");
            _maxValue = serializedObject.FindProperty("maxValue");
            _currentValue = serializedObject.FindProperty("currentValue");
            _isWholeValue = serializedObject.FindProperty("isWholeValue");
            _showDetailSetting = serializedObject.FindProperty("showDetailSetting");
        }
    
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            
            EditorGUILayout.PropertyField(_minValue); 
            EditorGUILayout.PropertyField(_maxValue);
            EditorGUILayout.PropertyField(_isWholeValue, new GUIContent("Whole Numbers"));
            
            EditorGUILayout.Slider(_currentValue, _minValue.floatValue, _maxValue.floatValue, new GUIContent("Current Value"));
            if (_isWholeValue.boolValue)
            {
                _currentValue.floatValue = Mathf.Round(_currentValue.floatValue);
            }
            
            _showDetailSetting.boolValue = EditorGUILayout.Foldout(_showDetailSetting.boolValue, "Other Settings");
            if (_showDetailSetting.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_fill, new GUIContent("Fill Image"));
                EditorGUILayout.PropertyField(_valueText, new GUIContent("Value Text"));
                EditorGUILayout.PropertyField(_handle, new GUIContent("Handle Image"));
                EditorGUILayout.PropertyField(_valueFormat);
                EditorGUI.indentLevel--;
            }
            
            if (GUI.changed)
            {
                _slider.SetValue(_currentValue.floatValue);
            }
        
            serializedObject.ApplyModifiedProperties();
        }
        
        [MenuItem("GameObject/FrameworkUI/UISlider", false, 6)]
        private static void CreateCustomSlider()
        {
            var slider = new GameObject("@UISlider");
            Undo.RegisterCreatedObjectUndo(slider, "Create Custom Slider");
            
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
            GameObjectUtility.SetParentAndAlign(slider, parent);

            var sliderRect = slider.AddComponent<RectTransform>();
            sliderRect.anchoredPosition = Vector2.zero;
            var image = slider.AddComponent<Image>();
            var sliderComp = slider.AddComponent<UISlider>();
            sliderComp.interactable = false;
            UIModuleEditorUtils.ApplyPreset(sliderComp, "UISlider");

            var fill = new GameObject("Fill");
            Undo.RegisterCreatedObjectUndo(fill, "Create Fill");
            var fillRect = fill.AddComponent<RectTransform>();
            fillRect.SetParent(slider.transform, false);
            var fillImage = fill.AddComponent<Image>();

            var handle = new GameObject("Handle");
            Undo.RegisterCreatedObjectUndo(handle, "Create Handle");
            var handleRect = handle.AddComponent<RectTransform>();
            handleRect.SetParent(fill.transform, false);
            handleRect.anchorMax = new Vector2(0f, 0.5f);
            handleRect.anchorMin = new Vector2(0f, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;
            var handleImage = handle.AddComponent<Image>();
            handleImage.type = Image.Type.Simple;
            
            var value = new GameObject("Value");
            Undo.RegisterCreatedObjectUndo(value, "Create TMP Text");
            value.AddComponent<RectTransform>();
            var text = value.AddComponent<TextMeshProUGUI>();
            UIModuleEditorUtils.ApplyPreset(text, "TextMeshProUGUI");
            GameObjectUtility.SetParentAndAlign(value, slider);
            
            var uiSlider = slider.GetComponent<UISlider>();
            uiSlider.fill = fillImage;
            uiSlider.handle = handleImage;
            uiSlider.valueText = value.GetComponent<TextMeshProUGUI>();

            EditorSceneManager.MarkSceneDirty(slider.scene);
        }
    }
}
