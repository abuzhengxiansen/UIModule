using System;
using System.Collections.Generic;
using LiteQuark.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GamePlay
{
    public abstract class BaseWidget : ITick
    {
        public UIStatus Status { get; set; }
        public string Name { get; private set; }
        public Transform Tf { get; private set; }
        public GameObject Go { get; private set; }
        public RectTransform Rect { get; private set; }
        public BaseWidget Parent { get; private set; }
        protected Animator UIAnimator { get; private set; }
        protected UIBaseBinder UIBinder { get; private set; }

        private int _eventTag;
        private ICustomUIData _data;
        private readonly Dictionary<string, UnityEngine.Object> _cacheAssets = new();
        private readonly Dictionary<string, BaseWidget> _widgetCacheDict = new();
        private readonly List<BaseWidget> _widgetCacheList = new();
        private readonly Queue<BaseWidget> _pendingDisposeWidgetQueue = new ();
        private readonly Dictionary<string, List<Action<BaseWidget>>> _widgetCreateCallbacks = new();
        
        public T GetCustomData<T>() where T : ICustomUIData
        {
            if (_data is T customData)
            {
                return customData;
            }
            
            return default;
        }

        #region virtual methods

        protected virtual void OnCreate(){}
        protected virtual void OnDispose(){}
        
        protected virtual void OnUpdate(float deltaTime){}
        
        public virtual void CloseSelf(Action callBack = null)
        {
            Parent.DisposeWidget(Name, callBack);
        }
        
        protected virtual void OnAnimatorPlayOver(string actionName){}

        #endregion

        #region internal

        internal virtual void Create()
        {
            Status = UIStatus.Opening;
            _eventTag = Go.name.GetHashCode();
            GenerateAutoCode();
            OnCreate();
        }
        
        internal virtual void Dispose()
        {
            Status = UIStatus.Disposing;
            
            UnRegisterAllEvents();
            DisposeAllWidgets();
            OnDispose();
        }
        
        internal virtual void DisposeImmediately()
        {
            Status = UIStatus.Sleeping;

            UIBinder.AnimOverAction = null;
            DisposePendingQueueImmediately();
            UnloadAllAssets();
        }

        public void Tick(float deltaTime)
        {
            DisposePendingQueueImmediately();
            
            for (var i = _widgetCacheList.Count - 1; i >= 0; i--)
            {
                if (i >= _widgetCacheList.Count) continue;
                var widget = _widgetCacheList[i];
                if (widget.Status is UIStatus.Disposing or UIStatus.Sleeping)
                {
                    _widgetCacheList.RemoveAt(i);
                    continue;
                }
                widget.Tick(deltaTime);
            }
            
            OnUpdate(deltaTime);
        }
        
        internal void SetWidgetData(string name, GameObject obj, BaseWidget parent = null)
        {
            Status = UIStatus.Sleeping;
            Name = name;
            Parent = parent;
            Go = obj;
            Go.name = name;
            Tf = Go.transform;
            Rect = Go.GetComponent<RectTransform>();
            UIAnimator = Tf.GetComponent<Animator>();
            UIBinder = Tf.GetComponent<UIBaseBinder>();
            if (UIAnimator)
            {
                UIBinder.AnimOverAction = UIAnimOver;
            }
        }
        
        internal void SetCustomData(ICustomUIData data)
        {
            if (data == null) return;
            _data = data;
        }
        
        internal virtual void UIAnimOver(string actionName)
        {
            OnAnimatorPlayOver(actionName);
        }
        
        protected virtual void GenerateAutoCode(){}

        #endregion
        
        #region Asset
        
        protected virtual void LoadAsset<T>(string address, Action<T> callback) where T : UnityEngine.Object
        {
            if (_cacheAssets.TryGetValue(address, out var cacheAsset))
            {
                callback?.Invoke(cacheAsset as T);
                return;
            }
            
            LiteRuntime.Asset.LoadAssetAsync<T>(address, (asset) =>
            {
                if (!asset)
                {
                    callback?.Invoke(null);
                    return;
                }
                if (Status == UIStatus.Disposing || Status == UIStatus.Sleeping || !Go)
                {
                    LiteRuntime.Asset.UnloadAsset(asset);
                    callback?.Invoke(null);
                    return;
                }
                _cacheAssets[address] = asset;
                callback?.Invoke(asset);
            });
        }
        
        protected void UnloadAsset(string address)
        {
            if (!_cacheAssets.Remove(address))
            {
                return;
            }
            LiteRuntime.Asset.UnloadAsset(address);
        }
        
        private void UnloadAllAssets()
        {
            foreach (var (_, asset) in _cacheAssets)
            {
                LiteRuntime.Asset.UnloadAsset(asset);
            }
            _cacheAssets.Clear();

            if (Go)
            {
                LiteRuntime.Asset.UnloadAsset(Go);
                Go = null;
            }
        }
        
        #endregion
        
        #region widget
        
        public T GetWidget<T>(string widgetName) where T : BaseWidget
        {
            if (_widgetCacheDict.TryGetValue(widgetName, out var cacheWidget))
            {
                return cacheWidget as T;
            }
            return null;
        }

        public void CreateWidget<T>(string widgetName, Transform parent, string address, ICustomUIData data = null, Action<T> callback = null) where T : BaseWidget
        {
            if (_widgetCacheDict.TryGetValue(widgetName, out var cacheWidget))
            {
                callback?.Invoke(cacheWidget as T);
                return;
            }

            if (callback != null)
            {
                if (_widgetCreateCallbacks.TryGetValue(widgetName, out var callbacks))
                {
                    callbacks.Add(newWidget => callback((T)newWidget));
                    return;
                }
                _widgetCreateCallbacks[widgetName] = new List<Action<BaseWidget>> { newWidget => callback((T)newWidget) };
            }
            
            LiteRuntime.Asset.InstantiateAsync(address, parent, obj =>
            {
                if (!obj)
                {
                    LiteRuntime.Log.Error("Create widget failed, address: {0}", address);
                    callback?.Invoke(null);
                    return;
                }
                
                if (Status == UIStatus.Disposing || Status == UIStatus.Sleeping || !Go)
                {
                    LiteRuntime.Asset.UnloadAsset(obj);
                    return;
                }

                if (Activator.CreateInstance(typeof(T)) is not BaseWidget widget)
                {
                    LiteRuntime.Log.Error("Failed to create widget: {0}", widgetName);
                    return;
                }
                
                widget.SetWidgetData(widgetName, obj, this);
                widget.SetCustomData(data);
                widget.Create();
                widget.Status = UIStatus.Showing;
                
                _widgetCacheDict.Add(widgetName, widget);
                _widgetCacheList.Add(widget);
                
                if (_widgetCreateCallbacks.TryGetValue(widgetName, out var existingCallbacks))
                {
                    foreach (var existingCallback in existingCallbacks)
                    {
                        existingCallback?.Invoke(widget);
                    }
                    _widgetCreateCallbacks.Remove(widgetName);
                }
            });
        }
        
        public void CreateWidget(string widgetName, Type widgetType, Transform parent, string address, ICustomUIData data = null, Action<BaseWidget> callback = null)
        {
            if (_widgetCacheDict.TryGetValue(widgetName, out var cacheWidget))
            {
                callback?.Invoke(cacheWidget);
                return;
            }

            if (callback != null)
            {
                if (_widgetCreateCallbacks.TryGetValue(widgetName, out var callbacks))
                {
                    callbacks.Add(callback);
                    return;
                }
                _widgetCreateCallbacks[widgetName] = new List<Action<BaseWidget>> { callback };
            }
            
            LiteRuntime.Asset.InstantiateAsync(address, parent, obj =>
            {
                if (!obj)
                {
                    LiteRuntime.Log.Error("Create widget failed, address: {0}", address);
                    callback?.Invoke(null);
                    return;
                }
                
                if (Status == UIStatus.Disposing || Status == UIStatus.Sleeping || !Go)
                {
                    LiteRuntime.Asset.UnloadAsset(obj);
                    return;
                }

                if (Activator.CreateInstance(widgetType) is not BaseWidget widget)
                {
                    LiteRuntime.Log.Error("Failed to create widget: {0}", widgetName);
                    return;
                }
                
                widget.SetWidgetData(widgetName, obj, this);
                widget.SetCustomData(data);
                widget.Create();
                widget.Status = UIStatus.Showing;
                
                _widgetCacheDict.Add(widgetName, widget);
                _widgetCacheList.Add(widget);
                
                if (_widgetCreateCallbacks.TryGetValue(widgetName, out var existingCallbacks))
                {
                    foreach (var existingCallback in existingCallbacks)
                    {
                        existingCallback?.Invoke(widget);
                    }
                    _widgetCreateCallbacks.Remove(widgetName);
                }
            });
        }
        
        public T ActiveWidget<T>(string widgetName, GameObject obj, ICustomUIData data = null) where T : BaseWidget
        {
            if (_widgetCacheDict.TryGetValue(widgetName, out var cacheWidget))
            {
                return cacheWidget as T;
            }

            if (Activator.CreateInstance(typeof(T)) is not BaseWidget widget)
            {
                LiteRuntime.Log.Error("Failed to create widget: {0}", widgetName);
                return null;
            }
            widget.SetWidgetData(widgetName, obj, this);
            widget.SetCustomData(data);
            widget.Create();
            
            _widgetCacheDict.Add(widgetName, widget);
            _widgetCacheList.Add(widget);
            return widget as T;
        }
        
        public T CloneWidget<T>(string widgetName, GameObject temp, Transform parent, ICustomUIData data = null) where T : BaseWidget
        {
            if (_widgetCacheDict.TryGetValue(widgetName, out var cacheWidget))
            {
                return cacheWidget as T;
            }
            if (temp.activeSelf)
            {
                temp.SetActive(false);
            }
            if (Activator.CreateInstance(typeof(T)) is not BaseWidget widget)
            {
                LiteRuntime.Log.Error("Failed to create widget: {0}", widgetName);
                return null;
            }
            widget.SetWidgetData(widgetName, UnityEngine.Object.Instantiate(temp, parent), this);
            widget.SetCustomData(data);
            widget.Create();
            
            _widgetCacheDict.Add(widgetName, widget);
            _widgetCacheList.Add(widget);
            return widget as T;
        }
        
        public void DisposeWidget(string widgetName, Action callback = null)
        {
            if (!_widgetCacheDict.Remove(widgetName, out var widget)) return;
            _widgetCacheList.Remove(widget);
            _pendingDisposeWidgetQueue.Enqueue(widget);
            widget.Dispose();
            callback?.Invoke();
        }
        
        private void DisposeAllWidgets()
        {
            _widgetCacheList.Clear();
            _widgetCreateCallbacks.Clear();
            foreach (var (_, widget) in _widgetCacheDict)
            {
                widget.Dispose();
                _pendingDisposeWidgetQueue.Enqueue(widget);
            }
            _widgetCacheDict.Clear();
        }

        private void DisposePendingQueueImmediately()
        {
            while (_pendingDisposeWidgetQueue.Count > 0)
            {
                var widget = _pendingDisposeWidgetQueue.Dequeue();
                widget.DisposeImmediately();
            }
        }

        #endregion
        
        #region Event
        
        private void UnRegisterAllEvents()
        {
            LiteRuntime.Event.UnRegisterAll(_eventTag);
        }

        protected void RegisterEvent<T>(Action<T> action) where T : IEventData
        {
            LiteRuntime.Event.Register(_eventTag, action);
        }
        
        protected void UnRegisterEvent<T>(Action<T> action) where T : IEventData
        {
            LiteRuntime.Event.UnRegister(_eventTag, action);
        }

        #endregion

        #region extension

        #region Find
        
        public T FindComponent<T>(Transform root, string path = null) where T : Component
        {
            if (path == null)
            {
                return root.GetComponent<T>();
            }
            
            var transform = root.Find(path);
            if (!transform)
            {
                LiteRuntime.Log.Warn("FindComponent failed: not found {1} at path '{0}'", path, typeof(T).Name);
                return null;
            }
            return transform.GetComponent<T>();
        } 

        public T FindComponent<T>(string path = null) where T : Component
        {
            return FindComponent<T>(Tf, path);
        }

        public Transform FindTransform(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                LiteRuntime.Log.Warn("FindComponent failed: not found transform at path '{0}'", path);
                return null;
            }

            return FindComponent<Transform>(path);
        }

        #endregion
        
        #region SetActive

        public void SetActive(GameObject obj, bool active)
        {
            if (!obj) return;
            if (obj.activeSelf == active) return;
            obj.SetActive(active);
        }
        
        public void SetActive(Transform tf, bool active)
        {
            if (!tf) return;
            SetActive(tf.gameObject, active);
        }

        public void SetActive(bool active)
        {
            SetActive(Go, active);
        }

        public void SetActive(string path, bool active)
        {
            var transform = Tf.Find(path);
            if (!transform) return;
            SetActive(transform, active);
        }

        #endregion
        
        #region Position

        public void SetAnchoredPos(RectTransform rect, Vector2 position)
        {
            if (!rect)
            {
                LiteRuntime.Log.Warn("SetAnchoredPos failed: RectTransform is null.");
                return;
            }
            rect.anchoredPosition = position;
        }
        
        public void SetAnchoredPos(Transform transform, Vector2 position)
        {
            var rectTransform = transform.GetComponent<RectTransform>();
            if (!rectTransform)
            {
                LiteRuntime.Log.Warn("SetAnchoredPos failed: RectTransform is null.");
                return;
            }
            SetAnchoredPos(rectTransform, position);
        }

        #endregion
        
        #region Size

        protected void SetRectSize(Transform transform, Vector2 size)
        {
            var rectTransform = transform.GetComponent<RectTransform>();
            if (!rectTransform)
            {
                LiteRuntime.Log.Warn("SetSize failed: RectTransform is null.");
                return;
            }
            rectTransform.sizeDelta = size;
        }
        
        protected void SetRectSizeWidth(Transform transform, float value)
        {
            var rectTransform = transform.GetComponent<RectTransform>();
            if (!rectTransform)
            {
                LiteRuntime.Log.Warn("SetSizeWidth failed: RectTransform is null.");
                return;
            }
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, value);
        }
        
        protected void SetRectSizeHeight(Transform transform, float value)
        {
            var rectTransform = transform.GetComponent<RectTransform>();
            if (!rectTransform)
            {
                LiteRuntime.Log.Warn("SetSizeWidth failed: RectTransform is null.");
                return;
            }
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, value);
        }

        #endregion
        
        #region Font
        
        protected void SetFontMaterial(TextMeshProUGUI text, string address)
        {
            if (!text)
            {
                LiteRuntime.Log.Warn("SetFontMaterial failed: TextMeshProUGUI is null.");
                return;
            }

            if (string.IsNullOrEmpty(address))
            {
                LiteRuntime.Log.Warn("SetFontMaterial failed: address is null or empty.");
                return;
            }
            
            if (_cacheAssets.TryGetValue(address, out var asset))
            {
                text.fontSharedMaterial = asset as Material;
                return;
            }
            
            LoadAsset<Material>(address, material =>
            {
                if (!material)
                {
                    LiteRuntime.Log.Error("Failed to load material from address: {0}", address);
                    return;
                }
                
                text.fontSharedMaterial = material;
            });
        }

        #endregion
        
        #region Sprite
        
        public void SetSprite(Image image, string address)
        {
            if (!image)
            {
                LiteRuntime.Log.Warn("SetSprite failed: Image is null.");
                return;
            }

            if (string.IsNullOrEmpty(address))
            {
                LiteRuntime.Log.Warn("SetSprite failed: address is null or empty.");
                return;
            }
            
            if (_cacheAssets.TryGetValue(address, out var asset))
            {
                image.sprite = asset as Sprite;
                return;
            }

            image.enabled = false;
            LoadAsset<Sprite>(address, sprite =>
            {
                if (!sprite)
                {
                    LiteRuntime.Log.Warn("Failed to load sprite from address: {0}", address);
                    return;
                }

                if (image == null) return;
                image.sprite = sprite;
                image.enabled = true;
            });
        }

        public void SetSprite(string path, string address)
        {
            SetSprite(FindComponent<Image>(path), address);
        }
        
        #endregion

        #endregion
    }
}