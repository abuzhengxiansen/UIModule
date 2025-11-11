using System;
using GamePlay;
using LiteQuark.Runtime;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    public sealed class UIToggle : Selectable, IPointerClickHandler, ICanvasElement
    {
        private static float _lastClickEffectTime;
        private const string DefaultClickSound = "Default";

        #region 内部
        
        // 组件自定义
        public string clickSound = DefaultClickSound;
        public string clickDisableSound = DefaultClickSound;
        public float clickCooldownTime = 0.1f;
        public UIToggleGroup toggleGroup;
        public CanvasGroup checkMark;
        public TMPro.TextMeshProUGUI text;
        public bool IsOn => isOn;
        
        [SerializeField]
        private bool showDetailSetting;
        [SerializeField]
        private bool isOn;

        private Action<UIToggle> _OnClick;
        private Action<UIToggle> _OnDisableClick;
        private Action<UIToggle, bool> _OnChange;
        
        protected override void Awake()
        {
            base.Awake();
            if (toggleGroup != null)
            {
                toggleGroup.RegisterToggle(this);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsActive()) return;
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (Time.time - _lastClickEffectTime < clickCooldownTime) return;

            if (!interactable)
            {
                PlayAudio(clickDisableSound);
                _OnDisableClick?.Invoke(this);
            }
            else
            {
                PlayAudio(clickSound);
                if (toggleGroup != null)
                {
                    toggleGroup.SetToggle(this, !isOn);
                }
                else
                {
                    SetToggle(!isOn);
                }
                _OnClick?.Invoke(this);
            }

            _lastClickEffectTime = Time.time;
        }
        
        private void RefreshToggle()
        {
            if (checkMark == null) return;
            checkMark.alpha = isOn ? 1f : 0f;
        }
        
        private void PlayAudio(string sound)
        {
            if (string.IsNullOrEmpty(sound)) return;
            
            if (sound == DefaultClickSound)
            {
                var uiModule = LiteRuntime.Get<UIModule>();
                sound = uiModule?.Config?.defaultClickAudio;
                if (string.IsNullOrEmpty(sound)) return;
            }
            
            LiteRuntime.Get<UIModule>()?.PlayAudio(sound);
        }
        
        public void Rebuild(CanvasUpdate executing)
        {
#if UNITY_EDITOR
            if (executing == CanvasUpdate.Prelayout)
                _OnChange.Invoke(this, isOn);
#endif
        }

        public void LayoutComplete()
        { }

        public void GraphicUpdateComplete()
        { }
        
        #endregion

        #region 对外
        
        public void SetClickEvent(Action<UIToggle> action)
        {
            _OnClick = action;
        }

        public void SetDisableClickEvent(Action<UIToggle> action)
        {
            _OnDisableClick = action;
        }

        public void SetChangeEvent(Action<UIToggle, bool> action)
        {
            if (toggleGroup != null)
            {
                // 有group由group统一处理
                LiteRuntime.Get<UIModule>()?.LogError("UIToggle can't set change event when it's in a group, please use group SetChangeEvent instead.");
                return;
            }
            
            _OnChange = action;
        }

        public void SetToggle(bool value, UIToggleGroup group = null)
        {
            if (toggleGroup != null && group != toggleGroup)
            {
                if (group == null)
                {
                    LiteRuntime.Get<UIModule>()?.LogError("UIToggle can't set toggle when it's in a group, please use group SetToggle instead.");
                }
                return;
            }
            
            isOn = value;

            RefreshToggle();

            _OnChange?.Invoke(this, value);
            if (toggleGroup != null)
            {
                toggleGroup.TriggerGroupChange(this);
            }
        }
        
        #endregion
    }
}