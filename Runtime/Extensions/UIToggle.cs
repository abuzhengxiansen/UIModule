using System;
using GamePlay;
using LiteQuark.Runtime;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    public sealed class UIToggle : Selectable, IPointerClickHandler, ICanvasElement
    {
        public static event Action<UIToggle, bool> GlobalClickEvent;

        #region internal
        
        private static float _lastClickEffectTime;
        public float clickCooldownTime = 0.1f;
        public UIToggleGroup toggleGroup;
        public CanvasGroup checkMark;
        public TMPro.TextMeshProUGUI text;
        public bool IsOn => isOn;
        public string customContent;
        
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
                OnGlobalClickEvent(this, false);
                _OnDisableClick?.Invoke(this);
            }
            else
            {
                OnGlobalClickEvent(this, true);
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
        
        private static void OnGlobalClickEvent(UIToggle obj, bool isValid)
        {
            GlobalClickEvent?.Invoke(obj, isValid);
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

        #region public
        
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