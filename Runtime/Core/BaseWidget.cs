using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GamePlay
{
    public abstract class BaseWidget : ITick
    {
        private class CacheAsset
        {
            public readonly Object Asset;
            public int Count;
            
            public CacheAsset(Object asset, int count)
            {
                Asset = asset;
                Count = count;
            }
        }
        
        public UIStatus Status { get; internal set; }
        public string Name { get; private set; }
        public Transform Tf { get; private set; }
        public GameObject Go { get; private set; }
        public RectTransform Rect { get; private set; }
        public BaseWidget Parent { get; private set; }
        public bool IsClone { get; private set; }
        protected Animator UIAnimator { get; private set; }
        protected UIBaseBinder UIBinder { get; private set; }

        private int _eventTag;
        private ICustomUIData _data;
        private readonly Dictionary<string, CacheAsset> _cacheAssets = new();
        private readonly Dictionary<string, BaseWidget> _widgetCacheDict = new();
        private readonly List<BaseWidget> _widgetCacheList = new();
        private readonly Dictionary<string, List<Action<BaseWidget>>> _widgetCreateCallbacks = new();
        private bool _needRemoveWidget;
        
        public T GetCustomData<T>() where T : ICustomUIData
        {
            if (_data is T customData)
            {
                return customData;
            }
            
            LiteRuntime.Get<UIModule>().LogWarn("GetCustomData failed: cannot cast to current type");
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
            if (Status != UIStatus.Creating) return;
            Status = UIStatus.Created;
            _eventTag = Go.GetInstanceID();
            GenerateAutoCode();
            OnCreate();
        }
        
        internal virtual void Dispose()
        {
            if (Status == UIStatus.Disposed) return;
            Status = UIStatus.Disposed;
            DisposeAllWidgets();
            OnDispose();
        }
        
        internal virtual void DisposeImmediately()
        {
            if (UIBinder)
            {
                UIBinder.AnimOverAction = null;
            }
            UnRegisterAllEvents();
            DisposeWidgetImmediately(true);
            UnloadAllAssets();
        }

        public void Tick(float deltaTime)
        {
            DisposeWidgetImmediately(false);

            foreach (var widget in _widgetCacheList)
            {
                if (widget.Status is >= UIStatus.Created and < UIStatus.Disposed)
                {
                    widget.Tick(deltaTime);
                }
            }
            
            OnUpdate(deltaTime);
        }
        
        internal void SetWidgetData(string name, BaseWidget parent = null)
        {
            Status = UIStatus.Creating;
            Name = name;
            Parent = parent;
        }

        internal void SetWidgetGo(GameObject obj)
        {
            Go = obj;
            Go.name = Name;
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
        
        protected virtual void LoadAsset<T>(string address, Action<T> callback) where T : Object
        {
            if (_cacheAssets.TryGetValue(address, out var cache))
            {
                // 已加载到资源
                if (cache.Asset != null && cache.Asset is T result)
                {
                    callback?.Invoke(result);
                    return;
                }

                // 资源正在加载中，增加加载计数
                cache.Count++;
            }
            else
            {
                // 占位记录
                _cacheAssets.Add(address, new CacheAsset(null, 1));
            }
            
            LiteRuntime.Asset.LoadAssetAsync<T>(address, (asset) =>
            {
                if (!asset)
                {
                    _cacheAssets[address].Count--;
                    callback?.Invoke(null);
                    return;
                }
                if (Status is < UIStatus.Created or >= UIStatus.Disposed)
                {
                    _cacheAssets[address].Count--;
                    LiteRuntime.Asset.UnloadAsset(asset);
                    callback?.Invoke(null);
                    return;
                }
                
                callback?.Invoke(asset);
            });
        }
        
        protected void UnloadAsset(string address)
        {
            if (!_cacheAssets.Remove(address, out var cache)) return;

            for (var i = 0; i < cache.Count; i++)
            {
                LiteRuntime.Asset.UnloadAsset(address);
            }
        }
        
        private void UnloadAllAssets()
        {
            foreach (var (path, cache) in _cacheAssets)
            {
                for (var i = 0; i < cache.Count; i++)
                {
                    LiteRuntime.Asset.UnloadAsset(path);
                }
            }
            _cacheAssets.Clear();

            if (Go)
            {
                if (IsClone)
                {
                    Object.Destroy(Go);
                }
                else
                {
                    LiteRuntime.Asset.UnloadAsset(Go);
                }
                Go = null;
            }
        }
        
        #endregion
        
        #region widget
        
        public T GetWidget<T>(string widgetName) where T : BaseWidget
        {
            var widget = GetWidget(widgetName);
            if (widget is T typedWidget)
            {
                return typedWidget;
            }

            return null;
        }

        public BaseWidget GetWidget(string widgetName)
        {
            return _widgetCacheDict.GetValueOrDefault(widgetName);
        }

        public void CreateWidget<T>(string widgetName, Transform parent, string address, ICustomUIData data = null, Action<T> callback = null) where T : BaseWidget
        {
            CreateWidget(widgetName, typeof(T), parent, address, data, newWidget => callback?.Invoke(newWidget as T));
        }
        
        public void CreateWidget(string widgetName, Type widgetType, Transform parent, string address, ICustomUIData data = null, Action<BaseWidget> callback = null)
        {
            if (Status == UIStatus.Disposed || Status == UIStatus.None || !Go)
            {
                LiteRuntime.Log.Warn("Cannot create widget '{0}' because parent widget is disposed or not initialized.", widgetName);
                return;
            }
            
            // 缓存记录检查，优先回调已存在的widget实例
            if (TryCheckWidgetCache(widgetName, callback)) return;
            AddWidgetCreateCallback(widgetName, callback);
            
            if (Activator.CreateInstance(widgetType) is not BaseWidget widget)
            {
                LiteRuntime.Log.Error("Failed to create widget: {0}", widgetName);
                return;
            }

            widget.Status = UIStatus.Creating;
            _widgetCacheDict.Add(widgetName, widget);
            _widgetCacheList.Add(widget);
            
            LiteRuntime.Asset.InstantiateAsync(address, parent, obj =>
            {
                if (!obj)
                {
                    LiteRuntime.Log.Error("Create widget failed, address: {0}", address);
                    DisposeWidget(widgetName);
                    callback?.Invoke(null);
                    return;
                }

                if (widget.Status != UIStatus.Creating)
                {
                    LiteRuntime.Log.Warn("Widget '{0}' is no longer in creating status, cannot set up widget with instantiated object.", widgetName);
                    DisposeWidget(widgetName);
                    LiteRuntime.Asset.UnloadAsset(obj);
                    return;
                }
                
                widget.SetWidgetData(widgetName, this);
                widget.SetWidgetGo(obj);
                widget.SetCustomData(data);
                widget.Create();
                
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
        
        public UniTask<T> CreateWidgetAsync<T>(string widgetName, Transform parent, string address, ICustomUIData data = null) where T : BaseWidget
        {
            var tcs = new UniTaskCompletionSource<T>();
            CreateWidget<T>(widgetName, parent, address, data, widget => tcs.TrySetResult(widget));
            return tcs.Task;
        }

        public UniTask<BaseWidget> CreateWidgetAsync(string widgetName, Type widgetType, Transform parent, string address, ICustomUIData data = null)
        {
            var tcs = new UniTaskCompletionSource<BaseWidget>();
            CreateWidget(widgetName, widgetType, parent, address, data, widget => tcs.TrySetResult(widget));
            return tcs.Task;
        }
        
        public T ActiveWidget<T>(string widgetName, GameObject obj, ICustomUIData data = null) where T : BaseWidget
        {
            if (Status == UIStatus.Disposed || Status == UIStatus.None || !Go)
            {
                LiteRuntime.Log.Warn("Cannot active widget '{0}' because parent widget is disposed or not initialized.", widgetName);
                return null;
            }
            
            if (_widgetCacheDict.TryGetValue(widgetName, out var cacheWidget))
            {
                return cacheWidget as T;
            }

            if (Activator.CreateInstance(typeof(T)) is not BaseWidget widget)
            {
                LiteRuntime.Log.Error("Failed to create widget: {0}", widgetName);
                return null;
            }
            
            _widgetCacheDict.Add(widgetName, widget);
            _widgetCacheList.Add(widget);
            
            widget.SetWidgetData(widgetName, this);
            widget.SetWidgetGo(obj);
            widget.SetCustomData(data);
            widget.Create();
            
            return widget as T;
        }
        
        public T CloneWidget<T>(string widgetName, GameObject temp, Transform parent, ICustomUIData data = null) where T : BaseWidget
        {
            if (Status == UIStatus.Disposed || Status == UIStatus.None || !Go)
            {
                LiteRuntime.Log.Warn("Cannot clone widget '{0}' because parent widget is disposed or not initialized.", widgetName);
                return null;
            }
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
            
            _widgetCacheDict.Add(widgetName, widget);
            _widgetCacheList.Add(widget);
            
            widget.SetWidgetData(widgetName, this);
            widget.SetWidgetGo(Object.Instantiate(temp, parent));
            widget.SetCustomData(data);
            widget.IsClone = true;
            widget.Create();
            
            return widget as T;
        }
        
        public void DisposeWidget(string widgetName, Action callback = null)
        {
            if (!_widgetCacheDict.Remove(widgetName, out var widget)) return;
            if (widget.Status == UIStatus.Disposed) return;

            _needRemoveWidget = true;
            widget.Dispose();
            callback?.Invoke();
        }
        
        private void AddWidgetCreateCallback(string widgetName, Action<BaseWidget> callback)
        {
            if (callback == null) return;
            
            if (_widgetCreateCallbacks.TryGetValue(widgetName, out var callbacks))
            {
                callbacks.Add(callback);
            }
            else
            {
                _widgetCreateCallbacks[widgetName] = new List<Action<BaseWidget>> { callback };
            }
        }

        private bool TryCheckWidgetCache(string widgetName, Action<BaseWidget> callback = null)
        {
            if (!_widgetCacheDict.TryGetValue(widgetName, out var cacheWidget)) return false;
            
            if (cacheWidget.Status is >= UIStatus.Created and < UIStatus.Disposed)
            {
                callback?.Invoke(cacheWidget);
            }
            else
            {
                AddWidgetCreateCallback(widgetName, callback);
            }

            return true;
        }
        
        private void DisposeAllWidgets()
        {
            _needRemoveWidget = true;
            _widgetCacheDict.Clear();
            _widgetCreateCallbacks.Clear();
            foreach (var widget in _widgetCacheList)
            {
                widget.Dispose();
            }
        }

        private void DisposeWidgetImmediately(bool isAll)
        {
            if (!isAll && !_needRemoveWidget) return;
            _needRemoveWidget = false;
            
            for (var i = _widgetCacheList.Count - 1; i >= 0; i--)
            {
                var widget = _widgetCacheList[i];
                if (!isAll && widget.Status != UIStatus.Disposed) continue;
                widget.DisposeImmediately();
                _widgetCacheList.RemoveAt(i);
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