using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GamePlay
{
    public class UIModule : ISystem, ITick
    {
        private class UILayer
        {
            public int LayerIndex;
            public string LayerName;
            public Transform LayerTransform;
        }
        
        public Camera UiCamera { get; private set; }
        private readonly List<UILayer> _uiLayers = new();
        private readonly Dictionary<string, UILayer> _layerMap = new();
        private UICanvas _root;
        private readonly Dictionary<string, LinkedList<BaseView>> _uiViewList = new();
        private readonly HashSet<BaseView> _uiViewCaches = new();
        private readonly Queue<BaseView> _pendingDisposeViewQueue = new ();
        private Vector2 _adaptAnchorMin = Vector2.zero;
        private Vector2 _adaptAnchorMax = Vector2.one;
        
        /// <summary>
        /// 获取UI模块配置
        /// </summary>
        public UIModuleConfig Config => _root?.Config;

        #region internal
        
        public void Tick(float deltaTime)
        {
            DisposePendingQueueImmediately();
            
            foreach (var (_, list) in _uiViewList)
            {
                var currentNode = list.First;
                while (currentNode != null)
                {
                    var view = currentNode.Value;
                    if (view.Status != UIStatus.None && view.Status != UIStatus.Disposed) 
                    {
                        view.Tick(deltaTime);
                    }
    
                    currentNode = currentNode.Next;
                }
            }
        }

        private void InitUILayers()
        {
            // 从UILayerSettings创建所有层级
            if (Config == null || Config.layers == null || Config.layers.Count == 0)
            {
                Debug.LogError("UIModule: layers is not configured in UIModuleConfig!");
                return;
            }
            
            for (var i = 0; i < Config.layers.Count; i++)
            {
                var layerName = Config.layers[i];
                var layer = CreateLayer(i + 1, layerName);
                _uiLayers.Add(layer);
                _layerMap[layerName] = layer;
            }
        }
        
        private void DestroyLayers()
        {
            for (var i = 0; i < _uiLayers.Count; i++)
            {
                var layer = _uiLayers[i];
                if (layer.LayerTransform)
                {
                    Object.Destroy(layer.LayerTransform.gameObject);
                }
            }
            _uiLayers.Clear();
            _layerMap.Clear();
        }
        
        private UILayer GetLayer(string layerName)
        {
            return _layerMap.GetValueOrDefault(layerName);
        }
        
        private UILayer CreateLayer(int index, string layerName)
        {
            var layer = new UILayer()
            {
                LayerIndex = index,
                LayerName = layerName,
            };
            var layerTransform = new GameObject(layerName, typeof(RectTransform)).transform;
            layerTransform.SetParent(_root.transform);
            layer.LayerTransform = layerTransform;
                    
            // 调整RectTransform的位置和大小
            var rectTransform = layerTransform.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localPosition = Vector3.zero;
            rectTransform.localScale = Vector3.one;
            
            return layer;
        }

        private BaseView CreateUIView(UIConfig config)
        {
            var uiView = Activator.CreateInstance(config.Type) as BaseView;
            if (uiView == null) return null;
            
            uiView?.SetViewData(config);
            _uiViewCaches.Add(uiView);
            return uiView;
        }
        
        private void CreateUIToOpen(UIConfig config, ICustomUIData data, bool isImmediately = false, Action<bool> callback = null)
        {
            try
            {
                LiteRuntime.Event.Send(new UIOperateEvent(config.Type, config.Name, UIOperateState.PrepareCreate));
            
                var layer = GetLayer(config.Layer);
                if (layer == null)
                {
                    LogError($"CreateUIObjAsync failed: Layer {config.Layer} not found");
                    callback?.Invoke(false);
                    return;
                }
                
                var uiView = CreateUIView(config);
                if (uiView == null)
                {
                    LogError($"CreateUIObjAsync failed: Unable to create instance of {config.Name}");
                    callback?.Invoke(false);
                    return;
                }
                
                LiteRuntime.Asset.InstantiateAsync(config.Path, layer.LayerTransform, (uiObj) =>
                {
                    if (!uiObj)
                    {
                        LogWarn($"OpenUIAsync failed: {config.Name} prefab not found at {config.Path}");
                        callback?.Invoke(false);
                        return;
                    }
                
                    var canvas = uiObj.GetComponent<Canvas>();
                    canvas.sortingLayerName = "UI";
                    canvas.enabled = false;
                    
                    uiView.SetViewGo(uiObj);
                    LiteRuntime.Event.Send(new UIOperateEvent(uiView, UIOperateState.Created));
                    ShowUI(uiView, data, isImmediately, callback);
                });
            }
            catch (Exception e)
            {
                LogError($"OpenUIAsync failed for {config.Name}: {e.Message}\n{e.StackTrace}");
                callback?.Invoke(false);
            }
        }
        
        private void ShowUI(BaseView view, ICustomUIData data = null, bool isImmediately = false, Action<bool> callback = null)
        {
            if (view == null)
            {
                callback?.Invoke(false);
                return;
            }
                
            if (data != null)
            {
                view.SetCustomData(data);
            }
         
            LiteRuntime.Event.Send(new UIOperateEvent(view, UIOperateState.PrepareOpen));
            
            var topView = GetTopUI(view.Config.Layer);
            if (topView == null || topView != view)
            {
                PopStack(view);
                var curTopView = GetTopUI(view.Config.Layer);
                curTopView?.Covered(view);
                PushStack(view);
            }
            
            if (view.Status == UIStatus.Creating)
            {
                view.Create();
            }
            if (view.Status == UIStatus.Showed)
            {
                callback?.Invoke(true);
            }
            else
            {
                view.Show(isImmediately, callback);
            }
        }

        private void HideUI(BaseView uiView, bool isImmediately = false, Action callback = null)
        {
            if (uiView == null)
            {
                callback?.Invoke();
                return;
            }
            
            LiteRuntime.Event.Send(new UIOperateEvent(uiView, UIOperateState.PrepareClose));
            
            if (uiView.Status == UIStatus.Showed)
            {
                uiView.Hide(isImmediately, () =>
                {
                    DestroyUI(uiView, callback);
                });
            }
            else
            {
                DestroyUI(uiView, callback);
            }
        }
        
        private void PushStack(BaseView uiView)
        {
            var layerName = uiView.Config.Layer;
            if (!_uiViewList.TryGetValue(layerName, out var list))
            {
                list = new LinkedList<BaseView>();
                _uiViewList.Add(layerName, list);
            }
            
            list.AddLast(uiView);
            RefreshOrder(uiView);
        }
        
        private void PopStack(BaseView uiView)
        {
            var layerName = uiView.Config.Layer;
            if (!_uiViewList.TryGetValue(layerName, out var list)) return;
            list.Remove(uiView);
        }
        
        private void DestroyUI(BaseView uiView, Action callback = null)
        {
            if (uiView == null)
            {
                callback?.Invoke();
                return;
            }
            
            LiteRuntime.Event.Send(new UIOperateEvent(uiView, UIOperateState.Closed));
            _uiViewCaches.Remove(uiView);
            PopStack(uiView);
            _pendingDisposeViewQueue.Enqueue(uiView);
            callback?.Invoke();
            uiView.Dispose();
        }
        
        private void DisposePendingQueueImmediately()
        {
            while (_pendingDisposeViewQueue.Count > 0)
            {
                _pendingDisposeViewQueue.Dequeue().DisposeImmediately();
            }
        }

        private void RefreshOrder(BaseView uiView)
        {
            var curLayer = GetLayer(uiView.Config.Layer);
            if (curLayer == null) return;

            // 只刷新当前layer中的UI
            if (!_uiViewList.TryGetValue(curLayer.LayerName, out var list) || list.Count == 0) return;
            
            // 计算该layer的sortingOrder起始值
            var baseSortingOrder = curLayer.LayerIndex * Config.layerSortSpace;
            var uiSortSpace = Config.uiSortSpace;
            
            // 遍历该layer中的所有UI，按顺序分配sortingOrder
            var orderIndex = 0;
            var node = list.First;
            while (node != null)
            {
                var uiSort = baseSortingOrder + orderIndex * uiSortSpace;
                if (node.Value.Canvas.sortingOrder != uiSort)
                {
                    node.Value.Canvas.sortingOrder = uiSort;
                }
                orderIndex++;
                node = node.Next;
            }
        }
        
        internal void LogWarn(string message)
        {
            LiteRuntime.Log.Warn(message);
        }
        
        internal void LogError(string message)
        {
            LiteRuntime.Log.Error(message);
        }

        #endregion

        #region public
        
        public void SetSafeArea(Rect safeArea)
        {
            _adaptAnchorMin = safeArea.position;
            _adaptAnchorMax = safeArea.position + safeArea.size;
            
            _adaptAnchorMin.x /= Screen.width;
            _adaptAnchorMin.y /= Screen.height;
            _adaptAnchorMax.x /= Screen.width;
            _adaptAnchorMax.y /= Screen.height;
            
        }

        #region Open

        public void OpenUI(UIConfig config, ICustomUIData data, bool isImmediately, Action<bool> callback = null)
        {
            try
            {
                var layer = config.Layer;
                var topView = GetTopUI(layer);

                var uiView = GetUI(config.Name);
                if (config.IsMultiple || uiView == null)
                {
                    if (!config.IsCoexist && topView != null)
                    {
                        topView.Hide(false, () =>
                        {
                            CreateUIToOpen(config, data, false, callback);
                        });
                    }
                    else
                    {
                        CreateUIToOpen(config, data, false, callback);
                    }
                }
                else if (topView != null && topView == uiView)
                {
                    LogWarn($"OpenUI: {config.Name} is already the top view can't open");
                    callback?.Invoke(false);
                }
                else if (uiView.Status == UIStatus.Creating)
                {
                    LogWarn($"OpenUI: {config.Name} is creating, please wait");
                    callback?.Invoke(false);
                }
                else
                {
                    if (!config.IsCoexist && topView != null)
                    {
                        topView.Hide(false, () =>
                        {
                            ShowUI(uiView, data, false, callback);
                        });
                    }
                    else if (uiView.Status == UIStatus.Showed)
                    {
                        uiView.Hide(false, () =>
                        {
                            ShowUI(uiView, data, false, callback);
                        });
                    }
                    else
                    {
                        ShowUI(uiView, data, false, callback);
                    }
                }
            }
            catch (Exception e)
            {
                LogError($"OpenUI failed for {config.Name}: {e.Message}\n{e.StackTrace}");
                callback?.Invoke(false);
            }
        }
        
        public async UniTask<bool> OpenUIAsync(UIConfig config, ICustomUIData data, bool isImmediately = false)
        {
            var tcs = new UniTaskCompletionSource<bool>();
            
            OpenUI(config, data, isImmediately, (success) =>
            {
                tcs.TrySetResult(success);
            });
            
            return await tcs.Task;
        }

        #endregion

        #region Close

        public void CloseUI(BaseView view, bool isImmediately = false, Action callback = null)
        {
            HideUI(view, isImmediately, () =>
            {
                var topView = GetTopUI(view.Config.Layer);
                if (topView != null && topView.Status != UIStatus.Showed)
                {
                    topView.Show(isImmediately, b =>
                    {
                        callback?.Invoke();
                    });
                }
                else
                {
                    callback?.Invoke();
                }
            });
        }

        #endregion

        #region GetUI

        public BaseView GetTopUI(string layerName = null)
        {
            if (string.IsNullOrEmpty(layerName))
            {
                for (var i = _uiLayers.Count - 1; i >= 0; i--)
                {
                    var layer = _uiLayers[i];
                    if (_uiViewList.TryGetValue(layer.LayerName, out var list) && list.Count > 0)
                    {
                        return list.Last.Value;
                    }
                }
            }
            else
            {
                if (!_uiViewList.TryGetValue(layerName, out var list))
                {
                    return null;
                }
                return list.Last?.Value;
            }
            return null;
        }
        
        public BaseView GetUI(Type type)
        {
            var uiName = type.Name;
            return GetUI(uiName);
        }
        
        public BaseView GetUI(string uiName)
        {
            BaseView uiView = null;
            foreach (var cacheView in _uiViewCaches)
            {
                if (cacheView.Name == uiName)
                {
                    uiView = cacheView;
                    break;
                }
            }
            if (uiView == null) return null;

            if (uiView.Config.IsMultiple)
            {
                if (!_uiViewList.TryGetValue(uiView.Config.Layer, out var list)) return null;
                for (var i = list.Count - 1; i >= 0; i--)
                {
                    var view = list.ElementAt(i);
                    if (view.Name == uiName)
                    {
                        uiView = view;
                        break;
                    }
                }
            }
            
            return uiView;
        }
        
        public T GetUI<T>() where T : BaseView
        {
            var uiView = GetUI(typeof(T));
            return uiView as T;
        }

        #endregion

        public void CloseUIByLayer(string layerName)
        {
            if (!_uiViewList.TryGetValue(layerName, out var list)) return;
            var node = list.First;
            while (node != null)
            {
                var view = node.Value;
                if (view.Status is not UIStatus.Disposed)
                {
                    _uiViewCaches.Remove(view);
                    _pendingDisposeViewQueue.Enqueue(view);
                    view.Dispose();
                }
                node = node.Next;
            }
            list.Clear();
            DisposePendingQueueImmediately();
        }

        public void CloseAllUI()
        {
            for (var i = 0; i < _uiLayers.Count; i++)
            {
                CloseUIByLayer(_uiLayers[i].LayerName);
            }
            _uiViewCaches.Clear();
            _uiViewList.Clear();
        }
        
        public void FilterScreenAdapt(RectTransform rectTrans)
        {
            rectTrans.anchorMin = _adaptAnchorMin;
            rectTrans.anchorMax = _adaptAnchorMax;
        }

        public Vector2 TransformToLocalPos(Vector2 pos, RectTransform rect)
        {
            var screenPos = RectTransformUtility.WorldToScreenPoint(UiCamera, pos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPos, UiCamera, out var localPos);
            return localPos;
        }

        #endregion
        
        public static UIModule Instance => LiteRuntime.Get<UIModule>();
        
        public UniTask<bool> Initialize()
        {
            _root = Object.FindObjectOfType<UICanvas>();
            var canvas = _root.GetComponent<Canvas>();
            if (canvas != null)
            {
                UiCamera = canvas.worldCamera;
            }
            
            SetSafeArea(Screen.safeArea);
            InitUILayers();
            
            return UniTask.FromResult(true);
        }

        public void Dispose()
        {
            CloseAllUI();
            DisposePendingQueueImmediately();
            DestroyLayers();
            _root = null;
        }
    }
    
    public struct UIConfig
    {
        public Type Type;
        public string Name;
        public string Layer;
        public string Path;
        public bool IsMultiple;
        public bool IsCoexist;
    }
}