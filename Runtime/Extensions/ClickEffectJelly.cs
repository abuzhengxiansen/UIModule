using UnityEngine.EventSystems;
using LiteQuark.Runtime;

namespace UnityEngine.UI
{
    public class ClickEffectJelly : Selectable
    {
        public Transform effectRoot;
        
        private Transform EffectTrans => effectRoot ? effectRoot : transform;
        private bool _effectShow;
        private Vector3 _initialScale;
        private ulong _actionId;
        // private bool _isPress;
        
        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            
            // _isPress = true;
            if (_effectShow) return;
            if (!IsActive() || !IsInteractable()) return;
            
            ShowStartTween();
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            
            // if (!_isPress) return;
            // if (!_effectShow) return;
            // if (!IsActive() || !IsInteractable()) return;
            //
            // ShowEndTween();
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            
            // _isPress = false;
            if (!_effectShow) return;
            if (!IsActive() || !IsInteractable()) return;
            
            ShowEndTween();
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            
            // if (!_isPress) return;
            // if (_effectShow) return;
            // if (!IsActive() || !IsInteractable()) return;
            //
            // ShowStartTween();
        }
        
        private void ShowStartTween()
        {
            _effectShow = true;
            
            if (_actionId != 0)
            {
                StopTween();
            }
            else
            {
                _initialScale = EffectTrans.localScale;
            }

            var action = ActionBuilder.Sequence("click effect jelly")
                .TransformScale(EffectTrans, _initialScale * 0.9f, 0.1f).Flush();
            _actionId = LiteRuntime.Action.AddAction(action, true);
        }
        
        private void ShowEndTween()
        {
            _effectShow = false;

            StopTween();
            
            // var action = ActionBuilder.Sequence("click effect jelly")
            //     .TransformScale(EffectTrans, _initialScale, 0.1f).Flush();
            // _actionId = LiteRuntime.Action.AddAction(action, true);
            
            EffectTrans.localScale = _initialScale;
        }
        
        private void StopTween()
        {
            if (_actionId == 0) return;
            LiteRuntime.Action.StopAction(_actionId);
            EffectTrans.localScale = _initialScale;
            _actionId = 0;
        }
    }
}