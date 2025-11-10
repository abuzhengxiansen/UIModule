using UnityEngine.EventSystems;
using LiteQuark.Runtime;

namespace UnityEngine.UI
{
    public class ClickEffectJelly : ClickEffectSingle, IPointerDownHandler, IPointerExitHandler, IPointerUpHandler, IPointerEnterHandler
    {
        public Transform effectRoot;
        
        private Transform EffectTrans => effectRoot ? effectRoot : transform;
        private bool _effectShow;
        private Vector3 _initialScale;
        private ulong _actionId;
        // private bool _isPress;
        
        public void OnPointerDown(PointerEventData eventData)
        {
            // _isPress = true;
            if (_effectShow) return;
            if (!Selectable.IsActive() || !Selectable.IsInteractable()) return;
            
            ShowStartTween();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // if (!_isPress) return;
            // if (!_effectShow) return;
            // if (!Selectable.IsActive() || !Selectable.IsInteractable()) return;
            //
            // ShowEndTween();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // _isPress = false;
            if (!_effectShow) return;
            if (!Selectable.IsActive() || !Selectable.IsInteractable()) return;
            
            ShowEndTween();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // if (!_isPress) return;
            // if (_effectShow) return;
            // if (!Selectable.IsActive() || !Selectable.IsInteractable()) return;
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