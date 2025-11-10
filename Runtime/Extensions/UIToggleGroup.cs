using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    public sealed class UIToggleGroup : UIBehaviour
    {
        #region 内部

        public UIToggle defaultToggle;
        public bool allowAllOff = true;
        public bool isDynamic;
        public UIToggle templateToggle;
        public Transform content;
        
        private Action<UIToggle, bool> _onChange;
        private int _totalCount;
        private int _showCount;
        private readonly List<UIToggle> _toggles = new ();
        private readonly Queue<UIToggle> _hideToggles = new ();
        private Action<Transform, int> _onRefresh;

        public void SetDefaultToggle(UIToggle toggle = null)
        {
            if (toggle != null)
            {
                defaultToggle = toggle;
            }
            if (defaultToggle == null) return;
            SetToggle(defaultToggle, true);
        }
        
        public void TriggerGroupChange(UIToggle toggle)
        {
            _onChange?.Invoke(toggle, toggle.IsOn);
        }

        public void SetToggle(UIToggle toggle, bool isOn)
        {
            if (toggle.IsOn == isOn) return;
            if (!allowAllOff && !isOn) return;
            
            toggle.SetToggle(isOn, this);
            foreach (var t in _toggles)
            {
                if (t == toggle) continue;
                if (defaultToggle == t && !isOn && !allowAllOff) continue;
                if (!t.IsOn) continue;
                t.SetToggle(false, this);
            }
        }
        
        private void UnregisterToggle(UIToggle toggle)
        {
            if (!_toggles.Contains(toggle)) return;
            toggle.toggleGroup = null;
            _toggles.Remove(toggle);
        }

        private bool AnyTogglesOn()
        {
            return _toggles.Find(x => x.IsOn) != null;
        }

        #endregion

        #region 对外
        
        public void SetChangeEvent(Action<UIToggle, bool> action)
        {
            _onChange = action;
        }
        
        public void SetRefreshToggleEvent(Action<Transform, int> action)
        {
            _onRefresh = action;
        }

        public void SetAllToggleOff()
        {
            foreach (var toggle in _toggles)
            {
                if (!allowAllOff && defaultToggle != null && _toggles.Count != 0 && toggle == defaultToggle)
                {
                    toggle.SetToggle(true, this);
                }
                toggle.SetToggle(false, this);
            }
        }
        
        public void SetTotalCount(int count, bool isRefresh = false)
        {
            _totalCount = count;
            if (isRefresh)
            {
                Refresh();
            }
        }
        
        public void RegisterToggle(UIToggle toggle)
        {
            if (_toggles.Contains(toggle)) return;
            toggle.toggleGroup = this;
            _toggles.Add(toggle);
            
            if (defaultToggle == null)
            {
                defaultToggle = toggle;
            }
        }

        public void Refresh()
        {
            if (!isDynamic) return;
            if (!content)
            {
                Debug.LogError("need set Content before Refresh");
                return;
            }
            if (!templateToggle)
            {
                Debug.LogError("need set TemplateToggle before Refresh");
                return;
            }
            if (_totalCount < 0)
            {
                Debug.LogError("need set TotalCount before Refresh");
                return;
            }
            if (_onRefresh == null)
            {
                Debug.LogError("need SetRefreshToggleEvent before Refresh");
                return;
            }
            
            if (_toggles.Count > _totalCount)
            {
                for (var i = _toggles.Count - 1; i >= _totalCount; i--)
                {
                    var toggle = _toggles[i];
                    UnregisterToggle(toggle);
                    _hideToggles.Enqueue(toggle);
                    toggle.gameObject.SetActive(false);
                }
                
                if (!allowAllOff && !AnyTogglesOn())
                {
                    _toggles[0].SetToggle(true, this);
                }
            }
            else if (_toggles.Count < _totalCount)
            {
                for (var i = _toggles.Count; i < _totalCount; i++)
                {
                    UIToggle toggle;
                    if (_hideToggles.Count > 0)
                    {
                        toggle = _hideToggles.Dequeue();
                    }
                    else
                    {
                        toggle = Instantiate(templateToggle, content);
                        toggle.transform.localScale = Vector3.one;
                    }
                    
                    toggle.name = $"Toggle_{i}";
                    RegisterToggle(toggle);
                    toggle.gameObject.SetActive(true);
                    _onRefresh?.Invoke(toggle.transform, i);
                }
            }
        }
        
        #endregion
    }
}