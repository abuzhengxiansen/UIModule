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
            public LayerType LayerType = LayerType.None;
            public int LayerOrder;
            public Transform LayerTransform;
        }
        
        private const int UiOrderSpace = 100;
        private const int LayerOrderSpace = 1000;
        public Camera UiCamera { get; private set; }
        private readonly List<UILayer> _uiLayers = new();
        private UICanvas _root;
        private readonly Dictionary<LayerType, LinkedList<BaseView>> _uiViewList = new();
        private readonly HashSet<BaseView> _uiViewCaches = new();
        private readonly Queue<BaseView> _pendingDisposeViewQueue = new ();
        private Vector2 _adaptAnchorMin = Vector2.zero;
        private Vector2 _adaptAnchorMax = Vector2.one;
        
        /// <summary>
        /// 获取UI模块配置
        /// </summary>
        public UIModuleConfig Config => _root?.Config;

        #region 内部
        
        public void SetSafeArea(Rect safeArea)
        {
            _adaptAnchorMin = safeArea.position;
            _adaptAnchorMax = safeArea.position + safeArea.size;
            
            _adaptAnchorMin.x /= Screen.width;
            _adaptAnchorMin.y /= Screen.height;
            _adaptAnchorMax.x /= Screen.width;
            _adaptAnchorMax.y /= Screen.height;
            
        }

        private void InitUILayers()
        {
            // 遍历LayerType枚举创建所有层级
            foreach (LayerType layerType in Enum.GetValues(typeof(LayerType)))
            {
                if (layerType == LayerType.None) continue;
                var order = (int)layerType * LayerOrderSpace;
                _uiLayers.Add(CreateLayer(layerType, order));
            }
        }
        
        private void DestroyLayers()
        {
            foreach (var layer in _uiLayers)
            {
                if (layer.LayerTransform)
                {
                    UnityEngine.Object.Destroy(layer.LayerTransform.gameObject);
                }
            }
            _uiLayers.Clear();
        }
        
        private UILayer GetLayer(LayerType layerType)
        {
            var layer = _uiLayers.Find(l => l.LayerType == layerType);
            return layer;
        }
        
        private UILayer CreateLayer(LayerType layerType, int order)
        {
            var layer = new UILayer() {
                LayerType = layerType,
                LayerOrder = order
            };
            var layerTransform = new GameObject(layer.LayerType.ToString(), typeof(RectTransform)).transform;
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
        
        private void CreateUIObjAsync(string path, LayerType layerType, Action<GameObject> callback)
        {
            var layer = GetLayer(layerType);
            LiteRuntime.Asset.InstantiateAsync(path, layer.LayerTransform, (uiObj) =>
            {
                if (!uiObj)
                {
                    callback?.Invoke(null);
                    return;
                }
                
                var canvas = uiObj.GetComponent<Canvas>();
                canvas.sortingLayerName = "UI";
                canvas.enabled = false;
                
                callback?.Invoke(uiObj);
            });
        }

        private BaseView CreateUIView(UIConfig config, GameObject uiObj)
        {
            var uiView = Activator.CreateInstance(config.Type) as BaseView;
            uiView?.SetViewData(config, uiObj);
            _uiViewCaches.Add(uiView);
            return uiView;
        }
        
        private void OpenUIAsync(UIConfig config, ICustomUIData data, Action callback = null)
        {
            LiteRuntime.Event.Send(new UIOperateEvent(config.Type, config.Name, UIOperateState.PrepareCreate));
            
            CreateUIObjAsync(config.Path, config.Layer, (uiObj) =>
            {
                if (!uiObj)
                {
                    LogWarn($"OpenUIAsync failed: {config.Type.Name} prefab not found at {config.Path}");
                    return;
                }
                
                var uiView = CreateUIView(config, uiObj);
                LiteRuntime.Event.Send(new UIOperateEvent(uiView, UIOperateState.Created));
                InternalOpenUI(uiView, data, callback);
            });
        }
        
        private void InternalOpenUI(BaseView view, ICustomUIData data, Action callback = null)
        {
            view.SetCustomData(data);
         
            LiteRuntime.Event.Send(new UIOperateEvent(view, UIOperateState.PrepareOpen));
            
            var topView = GetTopUI(view.Config.Layer);
            if (topView == null || topView != view)
            {
                PopStack(view);
                var curTopView = GetTopUI(view.Config.Layer);
                curTopView?.Covered(view);
                PushStack(view);
            }
            
            if (view.Status == UIStatus.Sleeping)
            {
                view.Create();
            }
            view.Show(callback);
        }

        private void InternalCloseUI(BaseView uiView, Action callback = null)
        {
            LiteRuntime.Event.Send(new UIOperateEvent(uiView, UIOperateState.PrepareClose));
            
            if (uiView.Status == UIStatus.Showing)
            {
                uiView.Hide(() =>
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
            var layer = uiView.Config.Layer;
            if (!_uiViewList.TryGetValue(layer, out var list))
            {
                list = new LinkedList<BaseView>();
                _uiViewList.Add(layer, list);
            }
            
            list.AddLast(uiView);
            RefreshOrder(uiView);
        }
        
        private void PopStack(BaseView uiView)
        {
            var layer = uiView.Config.Layer;
            if (!_uiViewList.TryGetValue(layer, out var list)) return;
            list.Remove(uiView);
        }
        
        private void DestroyUI(BaseView uiView, Action callback = null)
        {
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
            var layerType = uiView.Config.Layer;
            if (!_uiViewList.TryGetValue(layerType, out var list)) return;
            if (list.Count == 0) return;
            
            var layer = GetLayer(layerType);
            var order = layer.LayerOrder;
            foreach (var view in list)
            {
                view.Canvas.sortingOrder = order;
                order += UiOrderSpace;
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

        #region 对外接口

        public void Tick(float deltaTime)
        {
            DisposePendingQueueImmediately();
            
            foreach (var (_, list) in _uiViewList)
            {
                var currentNode = list.First;
                while (currentNode != null)
                {
                    var view = currentNode.Value;
                    if (view.Status != UIStatus.Sleeping && view.Status != UIStatus.Disposing) 
                    {
                        view.Tick(deltaTime);
                    }
    
                    currentNode = currentNode.Next;
                }
            }
        }

        public void OpenUI(UIConfig config, ICustomUIData data, Action callback = null)
        {
            var layer = config.Layer;
            var topView = GetTopUI(layer);

            var uiView = GetUI(config.Type);
            if (config.IsMultiple || uiView == null)
            {
                if (!config.IsCoexist && topView != null)
                {
                    topView.Hide(() =>
                    {
                        OpenUIAsync(config, data, callback);
                    });
                }
                else
                {
                    OpenUIAsync(config, data, callback);
                }
            }
            else if (topView != null && topView == uiView)
            {
                InternalOpenUI(uiView, data, callback);
            }
            else
            {
                if (!config.IsCoexist && topView != null)
                {
                    topView.Hide(() =>
                    {
                        InternalOpenUI(uiView, data, callback);
                    });
                }
                else
                {
                    InternalOpenUI(uiView, data, callback);
                }
            }
        }
        
        public void CloseUI(Type type, Action callback = null)
        {
            var uiView = GetUI(type);
            
            if (uiView == null || uiView.Status == UIStatus.Disposing) return;

            if (uiView.Config.IsMultiple)
            {
                LogWarn("CloseUI failed: Cannot close multiple instance UI by type.");
                return;
            }

            CloseUI(uiView, callback);
        }

        public void CloseUI(BaseView view, Action callback = null)
        {
            InternalCloseUI(view, () =>
            {
                var topView = GetTopUI(view.Config.Layer);
                if (topView != null && topView.Status != UIStatus.Showing)
                {
                    topView.Show();
                }
                callback?.Invoke();
            });
        }
        
        public BaseView GetTopUI(LayerType layerType = LayerType.None)
        {
            if (layerType == LayerType.None)
            {
                for (var i = _uiLayers.Count - 1; i >= 0; i--)
                {
                    var layer = _uiLayers[i];
                    if (_uiViewList.TryGetValue(layer.LayerType, out var list) && list.Count > 0)
                    {
                        return list.Last.Value;
                    }
                }
            }
            else
            {
                if (!_uiViewList.TryGetValue(layerType, out var list))
                {
                    return null;
                }
                return list.Last?.Value;
            }
            return null;
        }
        
        public T GetUI<T>() where T : BaseView
        {
            var uiView = GetUI(typeof(T));
            return uiView as T;
        }

        public BaseView GetUI(Type type)
        {
            var uiName = type.Name;
            var uiView = _uiViewCaches.FirstOrDefault(cache => cache.Name == uiName);
            if (uiView == null) return null;
            if (!_uiViewList.TryGetValue(uiView.Config.Layer, out var list)) return null;
            uiView = list.FirstOrDefault(view => view.Name == uiName);
            return uiView;
        }
        
        public BaseView GetUI(string name)
        {
            return _uiViewCaches.FirstOrDefault(view => view.Name == name);
        }

        public void CloseUIByLayer(LayerType layerType)
        {
            if (!_uiViewList.TryGetValue(layerType, out var list)) return;
            foreach (var view in list.Where(view => view.Status is not UIStatus.Disposing))
            {
                _uiViewCaches.Remove(view);
                _pendingDisposeViewQueue.Enqueue(view);
                view.Dispose();
            }
            list.Clear();
            DisposePendingQueueImmediately();
        }

        public void CloseAllUI()
        {
            foreach (var layer in _uiLayers)
            {
                CloseUIByLayer(layer.LayerType);
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

        /// <summary>
        /// 初始化 UI 模块
        /// </summary>
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
        public LayerType Layer;
        public string Path;
        public bool IsMultiple;
        public bool IsCoexist;
    }
}