using System;
using GamePlay;
using LiteQuark.Runtime;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    public class UIButton : Button
    {
        public enum ClickModes
        {
            Single,
            Double
        }
        public static event Action<UIButton, bool> GlobalClickEvent;
        
        #region internal
        
        private static float _lastClickEffectTime;
        public float clickCooldownTime = 0.1f;
        public float doubleClickEffectTime = 0.4f;
        public float pressEffectInterval = 0.2f;
        public float pressEffectTime = 0.8f;
        public ClickModes clickMode = ClickModes.Single;
        public bool isOpenPress;
        public string customContent;
        
        [SerializeField]
        private bool showDetailSetting;
        
        private Action<UIButton> _onClick;
        private Action<UIButton> _onDoubleClick;
        private Action<UIButton> _onPress;
        private Action<UIButton> _onDisableClick;
        private Action<UIButton> _onPointerDown;
        private Action<UIButton> _onPointerUp;
        private float _downTime;
        private float _lastClickTime;

        private void Update()
        {
            CheckPressed();
        }

        private void CheckPressed()
        {
            if (!IsPressed()) return;
            if (_onPress == null || !(Time.time - _downTime > pressEffectTime) || !(Time.time - _lastClickEffectTime > pressEffectInterval)) return;
            _onPress.Invoke(this);
            _lastClickEffectTime = Time.time;
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            
            _downTime = Time.time;
            
            _onPointerDown?.Invoke(this);
        }
        
        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            
            _downTime = 0;
            
            _onPointerUp?.Invoke(this);
        }
        
        public override void OnPointerClick(PointerEventData eventData)
        {
            base.OnPointerClick(eventData);
            
            if (Time.time - _lastClickEffectTime < clickCooldownTime) return;

            if (!interactable)
            {
                OnGlobalClickEvent(this, false);
                _onDisableClick?.Invoke(this);
            }
            else if (clickMode == ClickModes.Single)
            {
                OnGlobalClickEvent(this, true);
                _onClick?.Invoke(this);
                _lastClickEffectTime = Time.time;
            }
            else if (clickMode == ClickModes.Double && !Mathf.Approximately(Time.time, _lastClickTime) && Time.time - _lastClickTime < doubleClickEffectTime)
            {
                OnGlobalClickEvent(this, true);
                _onDoubleClick?.Invoke(this);
                _lastClickTime = Time.time;
            }

            _lastClickTime = Time.time;
        }
        
        private static void OnGlobalClickEvent(UIButton obj, bool isValid)
        {
            GlobalClickEvent?.Invoke(obj, isValid);
        }
        
        #endregion

        #region public
        
        public bool IsBtnPressed => IsPressed();
        
        public void SetClickEvent(Action<UIButton> action)
        {
            if (clickMode != ClickModes.Single)
            {
                LiteRuntime.Get<UIModule>()?.LogError("UIButton ClickType is not Single.");
                return;
            }
            _onClick = action;
        }
        
        public void SetDoubleClickEvent(Action<UIButton> action)
        {
            if (clickMode != ClickModes.Double)
            {
                LiteRuntime.Get<UIModule>()?.LogError("UIButton ClickType is not Double.");
                return;
            }
            _onDoubleClick = action;
        }

        public void SetPressEvent(Action<UIButton> action)
        {
            _onPress = action;
        }
        
        public void SetDisableClickEvent(Action<UIButton> action)
        {
            _onDisableClick = action;
        }
        
        public void SetPointerDownEvent(Action<UIButton> action)
        {
            _onPointerDown = action;
        }
        
        public void SetPointerUpEvent(Action<UIButton> action)
        {
            _onPointerUp = action;
        }
        
        #endregion
    }
}

