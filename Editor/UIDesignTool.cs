using UnityEditor.UIElements;
using UnityEngine;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEditor.Overlays;
using UnityEngine.UI;
using UnityEngine.UIElements;
using System;

namespace GamePlay.Editor
{
    [InitializeOnLoad]
    public class UIDesignTool
    {
        private const string PREFS_KEY = "UIDesignTool_Enabled_New";
        private const string NORMAL_STEP_KEY = "UIDesignTool_NormalStep";
        private const string SHIFT_STEP_KEY = "UIDesignTool_ShiftStep";
        private const string ALT_STEP_KEY = "UIDesignTool_AltStep";

        private static bool isEnabled = false;
        private static float normalStep = 1f;
        private static float shiftStep = 10f;
        private static float altStep = 0.1f;

        static UIDesignTool()
        {
            LoadEnabledState();
            LoadStepValues();
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                LoadEnabledState();
                LoadStepValues();
            }
        }

        private static void LoadEnabledState()
        {
            isEnabled = EditorPrefs.GetBool(PREFS_KEY, false);
        }

        private static void LoadStepValues()
        {
            normalStep = Mathf.Clamp(Mathf.Round(EditorPrefs.GetFloat(NORMAL_STEP_KEY, 1f)), 1, 9);
            shiftStep = Mathf.Max(10, Mathf.Round(EditorPrefs.GetFloat(SHIFT_STEP_KEY, 10f) / 10) * 10);
            altStep = Mathf.Clamp(EditorPrefs.GetFloat(ALT_STEP_KEY, 0.1f), 0.1f, 0.9f);
        }

        public static bool IsEnabled()
        {
            return isEnabled;
        }

        public static void SetEnabled(bool state)
        {
            isEnabled = state;
            EditorPrefs.SetBool(PREFS_KEY, isEnabled);
        }

        public static float GetNormalStep()
        {
            return normalStep;
        }

        public static float GetShiftStep()
        {
            return shiftStep;
        }

        public static float GetAltStep()
        {
            return altStep;
        }

        public static void SetSteps(float normal, float shift, float alt)
        {
            // 验证并修正普通步长 (1-9的整数)
            normalStep = Mathf.Clamp(Mathf.Round(normal), 1, 9);
            
            // 验证并修正Shift步长 (10的倍数)
            shiftStep = Mathf.Max(10, Mathf.Round(shift / 10) * 10);
            
            // 验证并修正Alt步长 (0.1-0.9的小数)
            altStep = Mathf.Clamp((float)Math.Round(alt, 1), 0.1f, 0.9f);
            
            EditorPrefs.SetFloat(NORMAL_STEP_KEY, normalStep);
            EditorPrefs.SetFloat(SHIFT_STEP_KEY, shiftStep);
            EditorPrefs.SetFloat(ALT_STEP_KEY, altStep);
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!isEnabled) return;
            HandleKeyEvent(Event.current);
        }

        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            if (!isEnabled) return;
            HandleKeyEvent(Event.current);
        }

        private static void HandleKeyEvent(Event e)
        {
            if (e.type != EventType.KeyDown || !e.control) return;
            if (Selection.activeTransform is not RectTransform rectTransform) return;

            ProcessKeyEvent(e, rectTransform);
        }

        private static void ProcessKeyEvent(Event e, RectTransform rectTransform)
        {
            bool shift = e.shift;
            bool alt = e.alt;
            float moveAmount = alt ? altStep : (shift ? shiftStep : normalStep);

            switch (e.keyCode)
            {
                // 当按下空格键时取整 RectTransform 的位置和大小
                case KeyCode.Space:
                    RoundRectTransformPosition(rectTransform);
                    RoundRectTransformSize(rectTransform);
                    e.Use();
                    break;

                case KeyCode.UpArrow:
                    MoveRectTransform(rectTransform, new Vector2(0, moveAmount));
                    e.Use();
                    break;

                case KeyCode.DownArrow:
                    MoveRectTransform(rectTransform, new Vector2(0, -moveAmount));
                    e.Use();
                    break;

                case KeyCode.LeftArrow:
                    MoveRectTransform(rectTransform, new Vector2(-moveAmount, 0));
                    e.Use();
                    break;

                case KeyCode.RightArrow:
                    MoveRectTransform(rectTransform, new Vector2(moveAmount, 0));
                    e.Use();
                    break;

                // Q 键，用于将 RectTransform 的大小设置为其图像的大小
                case KeyCode.Q:
                    UnityEngine.UI.Image image = rectTransform.GetComponent<UnityEngine.UI.Image>();
                    if (image != null && image.sprite != null)
                    {
                        SetRectTransformToSpriteSize(rectTransform, image.sprite);
                        e.Use();
                    }
                    break;
            }
        }

        [Shortcut("UIDesignTool/Move Up", KeyCode.UpArrow, ShortcutModifiers.Control)]
        private static void MoveUpShortcut()
        {
            if (!isEnabled) return;
            if (Selection.activeTransform is RectTransform rectTransform)
            {
                bool shift = (Event.current != null) ? Event.current.shift : false;
                bool alt = (Event.current != null) ? Event.current.alt : false;
                float moveAmount = alt ? altStep : (shift ? shiftStep : normalStep);
                MoveRectTransform(rectTransform, new Vector2(0, moveAmount));
            }
        }

        [Shortcut("UIDesignTool/Move Down", KeyCode.DownArrow, ShortcutModifiers.Control)]
        private static void MoveDownShortcut()
        {
            if (!isEnabled) return;
            if (Selection.activeTransform is RectTransform rectTransform)
            {
                bool shift = (Event.current != null) ? Event.current.shift : false;
                bool alt = (Event.current != null) ? Event.current.alt : false;
                float moveAmount = alt ? altStep : (shift ? shiftStep : normalStep);
                MoveRectTransform(rectTransform, new Vector2(0, -moveAmount));
            }
        }

        [Shortcut("UIDesignTool/Move Left", KeyCode.LeftArrow, ShortcutModifiers.Control)]
        private static void MoveLeftShortcut()
        {
            if (!isEnabled) return;
            if (Selection.activeTransform is RectTransform rectTransform)
            {
                bool shift = (Event.current != null) ? Event.current.shift : false;
                bool alt = (Event.current != null) ? Event.current.alt : false;
                float moveAmount = alt ? altStep : (shift ? shiftStep : normalStep);
                MoveRectTransform(rectTransform, new Vector2(-moveAmount, 0));
            }
        }

        [Shortcut("UIDesignTool/Move Right", KeyCode.RightArrow, ShortcutModifiers.Control)]
        private static void MoveRightShortcut()
        {
            if (!isEnabled) return;
            if (Selection.activeTransform is RectTransform rectTransform)
            {
                bool shift = (Event.current != null) ? Event.current.shift : false;
                bool alt = (Event.current != null) ? Event.current.alt : false;
                float moveAmount = alt ? altStep : (shift ? shiftStep : normalStep);
                MoveRectTransform(rectTransform, new Vector2(moveAmount, 0));
            }
        }

        [Shortcut("UIDesignTool/Round Image Size", KeyCode.Space, ShortcutModifiers.Control)]
        private static void RoundPositionShortcut()
        {
            if (!isEnabled) return;
            if (Selection.activeTransform is RectTransform rectTransform)
            {
                RoundRectTransformPosition(rectTransform);
                RoundRectTransformSize(rectTransform);
            }
        }

        [Shortcut("UIDesignTool/Set Image Size", KeyCode.Q, ShortcutModifiers.Control)]
        private static void SetImageSizeShortcut()
        {
            if (!isEnabled) return;
            if (Selection.activeTransform is RectTransform rectTransform)
            {
                UnityEngine.UI.Image image = rectTransform.GetComponent<UnityEngine.UI.Image>();
                if (image != null && image.sprite != null)
                {
                    SetRectTransformToSpriteSize(rectTransform, image.sprite);
                }
            }
        }

        private static void RoundRectTransformPosition(RectTransform rectTransform)
        {
            Undo.RecordObject(rectTransform, "Round Position");
            rectTransform.anchoredPosition3D = new Vector3(
                Mathf.Round(rectTransform.anchoredPosition3D.x),
                Mathf.Round(rectTransform.anchoredPosition3D.y),
                0
            );
            EditorUtility.SetDirty(rectTransform);
        }

        private static void RoundRectTransformSize(RectTransform rectTransform)
        {
            Undo.RecordObject(rectTransform, "Round Size");
            rectTransform.sizeDelta = new Vector2(
                Mathf.Round(rectTransform.sizeDelta.x),
                Mathf.Round(rectTransform.sizeDelta.y)
            );
            EditorUtility.SetDirty(rectTransform);
        }

        private static void MoveRectTransform(RectTransform rectTransform, Vector2 delta)
        {
            Undo.RecordObject(rectTransform, "Move Position");
            rectTransform.anchoredPosition += delta;
            EditorUtility.SetDirty(rectTransform);
        }

        private static void SetRectTransformToSpriteSize(RectTransform rectTransform, Sprite sprite)
        {
            Undo.RecordObject(rectTransform, "Set to Image Size");
            rectTransform.sizeDelta = new Vector2(sprite.rect.width, sprite.rect.height);
            EditorUtility.SetDirty(rectTransform);
        }

        [MenuItem("Edit/UI Design Tool/Toggle Plugin &g")]
        private static void MenuTogglePlugin()
        {
            SetEnabled(!IsEnabled());
            Debug.Log($"UI Design Tool {(IsEnabled() ? "Enabled" : "Disabled")}");
        }

        [MenuItem("Edit/UI Design Tool/Toggle Plugin &g", true)]
        private static bool MenuTogglePluginValidate()
        {
            Menu.SetChecked("Edit/UI Design Tool/Toggle Plugin &g", IsEnabled());
            return true;
        }
    }

    [Overlay(typeof(SceneView), "UI Design Tool", "UI Design Tool",
        defaultDisplay = true,
        defaultDockZone = DockZone.TopToolbar,
        defaultDockPosition = DockPosition.Top,
        defaultLayout = Layout.HorizontalToolbar)]
    [Icon("UnityEditor.TransformTool")]
    public class ToolbarOverlay : Overlay
    {
        private UnityEngine.UIElements.Button toggleButton;
        private UnityEngine.UIElements.Button dropdownButton;
        private VisualElement dropdownMenu;
        private bool isDropdownVisible = false;
        private bool isEnabled;

        public override VisualElement CreatePanelContent()
        {
            isEnabled = UIDesignTool.IsEnabled();

            var container = new VisualElement()
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    justifyContent = Justify.FlexStart,
                    paddingLeft = 5,
                    paddingRight = 5,
                    minWidth = 90,
                    height = 22,
                    backgroundColor = new Color(0.16f, 0.16f, 0.16f, 0.8f),
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                }
            };

            // 主按钮
            toggleButton = new UnityEngine.UIElements.Button(TogglePlugin)
            {
                text = isEnabled ? "UI Tool: ON" : "OFF",
                tooltip = "Toggle UI Design Tool",
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Normal,
                    fontSize = 11,
                    paddingLeft = 5,
                    paddingRight = 5,
                    paddingTop = 0,
                    paddingBottom = 0,
                    marginLeft = 0,
                    marginRight = 0,
                    borderLeftWidth = 0,
                    borderRightWidth = 0,
                    borderTopWidth = 0,
                    borderBottomWidth = 0,
                    backgroundColor = isEnabled ? new Color(0.2f, 0.6f, 0.2f, 0.3f) : new Color(0.6f, 0.2f, 0.2f, 0.3f),
                    unityTextAlign = TextAnchor.MiddleCenter,
                    whiteSpace = WhiteSpace.Normal,
                    width = 70
                }
            };

            // 下拉箭头按钮
            dropdownButton = new UnityEngine.UIElements.Button(ToggleDropdown)
            {
                text = "▼",
                tooltip = "Settings",
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Normal,
                    fontSize = 9,
                    paddingLeft = 2,
                    paddingRight = 2,
                    paddingTop = 0,
                    paddingBottom = 0,
                    marginLeft = 0,
                    marginRight = 0,
                    borderLeftWidth = 0,
                    borderRightWidth = 0,
                    borderTopWidth = 0,
                    borderBottomWidth = 0,
                    backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.3f),
                    unityTextAlign = TextAnchor.MiddleCenter,
                    width = 15
                }
            };

            // 下拉菜单
            dropdownMenu = new VisualElement()
            {
                style =
                {
                    position = Position.Absolute,
                    top = 23,
                    left = 0,
                    width = 180,
                    backgroundColor = new Color(0.2f, 0.2f, 0.2f),
                    borderTopWidth = 0,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    paddingTop = 8,
                    paddingBottom = 8,
                    paddingLeft = 10,
                    paddingRight = 10,
                    display = DisplayStyle.None
                }
            };

            // 添加步长设置控件 - 普通步长 (1-9的整数)
            var normalStepContainer = new VisualElement()
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    justifyContent = Justify.SpaceBetween,
                    marginBottom = 5
                }
            };
            
            var normalStepLabel = new Label("Normal Step:")
            {
                style =
                {
                    width = 80,
                    unityTextAlign = TextAnchor.MiddleLeft
                }
            };
            
            var normalStepField = new IntegerField()
            {
                value = (int)UIDesignTool.GetNormalStep(),
                style =
                {
                    width = 80,
                    marginLeft = 5
                }
            };
            normalStepField.RegisterValueChangedCallback(evt =>
            {
                int validatedValue = Mathf.Clamp(evt.newValue, 1, 9);
                if (evt.newValue != validatedValue)
                {
                    normalStepField.SetValueWithoutNotify(validatedValue);
                }
                UIDesignTool.SetSteps(validatedValue, UIDesignTool.GetShiftStep(), UIDesignTool.GetAltStep());
            });

            normalStepContainer.Add(normalStepLabel);
            normalStepContainer.Add(normalStepField);

            // 添加步长设置控件 - Shift步长 (10的倍数)
            var shiftStepContainer = new VisualElement()
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    justifyContent = Justify.SpaceBetween,
                    marginBottom = 5
                }
            };
            
            var shiftStepLabel = new Label("Shift Step:")
            {
                style =
                {
                    width = 80,
                    unityTextAlign = TextAnchor.MiddleLeft
                }
            };
            
            var shiftStepField = new IntegerField()
            {
                value = (int)UIDesignTool.GetShiftStep(),
                style =
                {
                    width = 80,
                    marginLeft = 5
                }
            };
            shiftStepField.RegisterValueChangedCallback(evt =>
            {
                int validatedValue = Mathf.Max(10, (int)(Mathf.Round(evt.newValue / 10f) * 10));
                if (evt.newValue != validatedValue)
                {
                    shiftStepField.SetValueWithoutNotify(validatedValue);
                }
                UIDesignTool.SetSteps(UIDesignTool.GetNormalStep(), validatedValue, UIDesignTool.GetAltStep());
            });

            shiftStepContainer.Add(shiftStepLabel);
            shiftStepContainer.Add(shiftStepField);

            // 添加步长设置控件 - Alt步长 (0.1-0.9的小数)
            var altStepContainer = new VisualElement()
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    justifyContent = Justify.SpaceBetween
                }
            };
            
            var altStepLabel = new Label("Alt Step:")
            {
                style =
                {
                    width = 80,
                    unityTextAlign = TextAnchor.MiddleLeft
                }
            };
            
            var altStepField = new FloatField()
            {
                value = UIDesignTool.GetAltStep(),
                style =
                {
                    width = 80,
                    marginLeft = 5
                }
            };
            altStepField.RegisterValueChangedCallback(evt =>
            {
                float validatedValue = Mathf.Clamp((float)Math.Round(evt.newValue, 1), 0.1f, 0.9f);
                if (Math.Abs(evt.newValue - validatedValue) > 0.001f)
                {
                    altStepField.SetValueWithoutNotify(validatedValue);
                }
                UIDesignTool.SetSteps(UIDesignTool.GetNormalStep(), UIDesignTool.GetShiftStep(), validatedValue);
            });

            altStepContainer.Add(altStepLabel);
            altStepContainer.Add(altStepField);

            dropdownMenu.Add(normalStepContainer);
            dropdownMenu.Add(shiftStepContainer);
            dropdownMenu.Add(altStepContainer);

            // 将按钮和下拉菜单添加到容器
            container.Add(toggleButton);
            container.Add(dropdownButton);
            container.Add(dropdownMenu);

            // 添加点击外部关闭下拉菜单的功能
            container.RegisterCallback<MouseDownEvent>(e =>
            {
                if (isDropdownVisible && !dropdownMenu.layout.Contains(e.localMousePosition))
                {
                    HideDropdown();
                }
            });

            return container;
        }

        private void TogglePlugin()
        {
            isEnabled = !isEnabled;
            UIDesignTool.SetEnabled(isEnabled);
            toggleButton.text = isEnabled ? "UI Tool: ON" : "OFF";
            toggleButton.style.backgroundColor = isEnabled ? 
                new Color(0.2f, 0.6f, 0.2f, 0.3f) : 
                new Color(0.6f, 0.2f, 0.2f, 0.3f);
            Debug.Log($"UI Design Tool {(isEnabled ? "Enabled" : "Disabled")}");
        }

        private void ToggleDropdown()
        {
            if (isDropdownVisible)
            {
                HideDropdown();
            }
            else
            {
                ShowDropdown();
            }
        }

        private void ShowDropdown()
        {
            isDropdownVisible = true;
            dropdownMenu.style.display = DisplayStyle.Flex;
        }

        private void HideDropdown()
        {
            isDropdownVisible = false;
            dropdownMenu.style.display = DisplayStyle.None;
        }

        public override void OnWillBeDestroyed()
        {
            if (toggleButton != null)
            {
                toggleButton.clickable = null;
            }
            if (dropdownButton != null)
            {
                dropdownButton.clickable = null;
            }
            base.OnWillBeDestroyed();
        }
    }
}