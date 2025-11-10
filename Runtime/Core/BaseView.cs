using System;
using GamePlay;
using LiteQuark.Runtime;
using UnityEngine;

namespace GamePlay
{
    public abstract class BaseView : BaseWidget
    {
        public Canvas Canvas { get; private set; }
        public UIConfig Config { get; private set; }
        
        private Action _showOverCallback;
        private Action _hideOverCallback;
        
        public void SetViewData(UIConfig config, GameObject go)
        {
            SetWidgetData(config.Name, go);
            
            Config = config;
            Canvas = Tf.GetComponent<Canvas>();
            AutoFilterAdaptRoot();
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

        public void Show(Action callBack = null)
        {
            if (Status is not UIStatus.Hiding and not UIStatus.Opening)
            {
                LiteRuntime.Log.Warn("{0} Show failed: ui status is need to be Hiding or Opening, but current status is {1}", Name, Status);
                return;
            }
            
            Canvas.enabled = true;
            
            DoShow(() =>
            {
                ShowOver();
                callBack?.Invoke();
            });
        }

        private void ShowOver()
        {
            Status = UIStatus.Showing;
            OnShow();
            
            LiteRuntime.Event.Send(new UIOperateEvent(this, UIOperateState.Opened));
        }
        
        protected virtual void DoShow(Action callBack)
        {
            if (UIAnimator)
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

        public override void CloseSelf(Action callBack = null)
        {
            LiteRuntime.Get<UIModule>().CloseUI(this, callBack);
        }

        public void Hide(Action callBack = null)
        {
            if (Status is not UIStatus.Showing)
            {
                LiteRuntime.Log.Warn("{0} Hide failed: ui status is need to be Showing, but current status is {1}", Name, Status);
                return;
            }
            
            DoHide(() =>
            {
                HideOver();
                callBack?.Invoke();
            });
        }

        private void HideOver()
        {
            Status = UIStatus.Hiding;
            Canvas.enabled = false;
            OnHide();
            LiteRuntime.Event.Send(new UIOperateEvent(this, UIOperateState.Closed));
        }

        protected virtual void DoHide(Action callBack)
        {
            if (UIAnimator)
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
    }
}