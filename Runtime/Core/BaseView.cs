using System;
using LiteQuark.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace GamePlay
{
    public abstract class BaseView : BaseWidget
    {
        public Canvas Canvas { get; private set; }
        public GraphicRaycaster Raycaster { get; private set; }
        public UIConfig Config { get; private set; }
        
        private Action _showOverCallback;
        private Action _hideOverCallback;
        private bool _useShowAnimator;
        private bool _useHideAnimator;
        
        public void SetViewData(UIConfig config, GameObject go)
        {
            SetWidgetData(config.Name, go);
            
            Config = config;
            Canvas = Tf.GetComponent<Canvas>();
            Raycaster = Tf.GetComponent<GraphicRaycaster>();
            
            AutoFilterAdaptRoot();
            ValidateUIAnimator();
        }
        
        public void Covered(BaseView view)
        {
            OnCovered(view);
        }
        
        protected virtual void OnCovered(BaseView view){}

        internal override void Dispose()
        {
            LiteRuntime.Event.Send(new UIOperateEvent(this, UIOperateState.Disposed));
            Canvas = null;
            base.Dispose();
        }

        internal override void UIAnimOver(string actionName)
        {
            base.UIAnimOver(actionName);
            
            if (actionName == "ShowUI")
            {
                _showOverCallback?.Invoke();
                _showOverCallback = null;
            }
            else if (actionName == "HideUI")
            {
                _hideOverCallback?.Invoke();
                _hideOverCallback = null;
            }
        }

        #region screen adaptation

        private void AutoFilterAdaptRoot()
        {
            for (var i = 0; i < Tf.childCount; i++)
            {
                var childTransform = Tf.GetChild(i);
                if (childTransform.name != "Root") continue;
                
                if (childTransform is RectTransform child)
                {
                    FilterScreenAdapt(child);
                }
            }
        }

        protected virtual void FilterScreenAdapt(RectTransform rectTrans)
        {
            if (!rectTrans)
            {
                LiteRuntime.Log.Warn("{0} FilterScreenAdapt failed: rectTrans is null", Name);
                return;
            }
            LiteRuntime.Get<UIModule>().FilterScreenAdapt(rectTrans);
        }

        #endregion

        #region show

        public void Show(bool isImmediately = false, Action<bool> callBack = null)
        {
            Status = UIStatus.Showing;
            Raycaster.enabled = false;
            Canvas.enabled = true;

            if (isImmediately)
            {
                ShowOver();
                callBack?.Invoke(true);
            }
            else
            {
                DoShow(() =>
                {
                    ShowOver();
                    callBack?.Invoke(true);
                });
            }
        }

        private void ShowOver()
        {
            Raycaster.enabled = true;
            Status = UIStatus.Showed;
            OnShow();
            
            LiteRuntime.Event.Send(new UIOperateEvent(this, UIOperateState.Opened));
        }
        
        protected virtual void DoShow(Action callBack)
        {
            if (_useShowAnimator)
            {
                _showOverCallback = callBack;
                UIAnimator.SetTrigger("ShowUI");
            }
            else
            {
                callBack?.Invoke();
            }
        }
        
        protected virtual void OnShow(){}

        #endregion
        
        #region hide

        public void Hide(bool isImmediately = false, Action callBack = null)
        {
            Status = UIStatus.Hiding;
            Raycaster.enabled = false;
            
            if (isImmediately)
            {
                HideOver();
                callBack?.Invoke();
            }
            else
            {
                DoHide(() =>
                {
                    HideOver();
                    callBack?.Invoke();
                });
            }
        }

        private void HideOver()
        {
            Status = UIStatus.Hided;
            Canvas.enabled = false;
            OnHide();
            LiteRuntime.Event.Send(new UIOperateEvent(this, UIOperateState.Closed));
        }

        protected virtual void DoHide(Action callBack)
        {
            if (_useHideAnimator)
            {
                _hideOverCallback = callBack;
                UIAnimator.SetTrigger("HideUI");
            }
            else
            {
                callBack?.Invoke();
            }
        }

        protected virtual void OnHide(){}

        #endregion
        
        #region Close
        
        public override void CloseSelf(Action callBack = null)
        {
            LiteRuntime.Get<UIModule>().CloseUI(this, false, callBack);
        }
        
        public void CloseImmediately(Action callBack = null)
        {
            LiteRuntime.Get<UIModule>().CloseUI(this, true, callBack);
        }

        #endregion
        
        #region animator
        
        private void ValidateUIAnimator()
        {
            if (!UIAnimator)
            {
                _useShowAnimator = false;
                _useHideAnimator = false;
                return;
            }
            
            _useShowAnimator = HasUIAnimatorTrigger("ShowUI");
            _useHideAnimator = HasUIAnimatorTrigger("HideUI");
        }
        
        private bool HasUIAnimatorTrigger(string triggerName)
        {
            if (!UIAnimator || !UIAnimator.runtimeAnimatorController) return false;
            
            foreach (var parameter in UIAnimator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Trigger && 
                    parameter.name == triggerName)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        #endregion
    }
}