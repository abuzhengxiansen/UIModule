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
            public int LayerId;
            public string LayerName;
            public Transform LayerTransform;
        }
        
        private const int UiOrderSpace = 100;
        public Camera UiCamera { get; private set; }
        private readonly List<UILayer> _uiLayers = new();
        private UICanvas _root;
        private readonly Dictionary<int, LinkedList<BaseView>> _uiViewList = new();
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
            if (Config == null || Config.layers.Count == 0)
            {
                Debug.LogError("UIModule: layers is not configured in UIModuleConfig!");
                return;
            }
            
            Config.layers.Sort((a, b) => a.layerOrder.CompareTo(b.layerOrder));
            
            foreach (var layerDefine in Config.layers)
            {
                _uiLayers.Add(CreateLayer(layerDefine));
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
        
        private UILayer GetLayer(int layerId)
        {
            var layer = _uiLayers.Find(l => l.LayerId == layerId);
            return layer;
        }
        
        private UILayer CreateLayer(UILayerDefine layerDefine)
        {
            var layer = new UILayer() {
                LayerId = layerDefine.layerId,
                LayerName = layerDefine.layerName,
            };
            var layerTransform = new GameObject(layer.LayerName, typeof(RectTransform)).transform;
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
        
        private void CreateUIObj(string path, int layerId, Action<GameObject> callback)
        {
            try
            {
                var layer = GetLayer(layerId);
                if (layer == null)
                {
                    LogError($"CreateUIObjAsync failed: Layer {layerId} not found");
                    callback?.Invoke(null);
                    return;
                }
                
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
            catch (Exception e)
            {
                LogError($"CreateUIObjAsync failed: {e.Message}\n{e.StackTrace}");
                callback?.Invoke(null);
            }
        }

        private BaseView CreateUIView(UIConfig config, GameObject uiObj)
        {
            var uiView = Activator.CreateInstance(config.Type) as BaseView;
            if (uiView != null)
            {
                uiView.Status = UIStatus.Creating;
            }
            uiView?.SetViewData(config, uiObj);
            _uiViewCaches.Add(uiView);
            return uiView;
        }
        
        private void CreateUIToOpen(UIConfig config, ICustomUIData data, bool isImmediately = false, Action<bool> callback = null)
        {
            try
            {
                LiteRuntime.Event.Send(new UIOperateEvent(config.Type, config.Name, UIOperateState.PrepareCreate));
            
                CreateUIObj(config.Path, config.Layer, (uiObj) =>
                {
                    if (!uiObj)
                    {
                        LogWarn($"OpenUIAsync failed: {config.Type.Name} prefab not found at {config.Path}");
                        callback?.Invoke(false);
                        return;
                    }
                
                    var uiView = CreateUIView(config, uiObj);
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
            
            if (view.Status == UIStatus.None)
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

            var orderNum = 0;
            foreach (var layer in _uiLayers)
            {
                var list = _uiViewList.GetValueOrDefault(layer.LayerId);
                if (list == null || list.Count == 0) continue;
                
                foreach (var view in list)
                {
                    view.Canvas.sortingOrder = orderNum;
                    orderNum += UiOrderSpace;
                }
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

                var uiView = GetUI(config.Type);
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
                    topView.Show();
                }
                callback?.Invoke();
            });
        }

        #endregion

        #region GetUI

        public BaseView GetTopUI(int layerId = -1)
        {
            if (layerId == -1)
            {
                for (var i = _uiLayers.Count - 1; i >= 0; i--)
                {
                    var layer = _uiLayers[i];
                    if (_uiViewList.TryGetValue(layer.LayerId, out var list) && list.Count > 0)
                    {
                        return list.Last.Value;
                    }
                }
            }
            else
            {
                if (!_uiViewList.TryGetValue(layerId, out var list))
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
            var uiView = _uiViewCaches.FirstOrDefault(cache => cache.Name == uiName);
            if (uiView == null) return null;

            if (uiView.Config.IsMultiple)
            {
                if (!_uiViewList.TryGetValue(uiView.Config.Layer, out var list)) return null;
                uiView = list.LastOrDefault(view => view.Name == uiName);
            }
            
            return uiView;
        }
        
        public BaseView GetUI(string name)
        {
            var uiView = _uiViewCaches.FirstOrDefault(cache => cache.Name == name);
            if (uiView == null) return null;

            if (uiView.Config.IsMultiple)
            {
                if (!_uiViewList.TryGetValue(uiView.Config.Layer, out var list)) return null;
                uiView = list.LastOrDefault(view => view.Name == name);
            }
            
            return uiView;
        }
        
        public T GetUI<T>() where T : BaseView
        {
            var uiView = GetUI(typeof(T));
            return uiView as T;
        }

        #endregion

        public void CloseUIByLayer(int layerId)
        {
            if (!_uiViewList.TryGetValue(layerId, out var list)) return;
            foreach (var view in list.Where(view => view.Status is not UIStatus.Disposed))
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
                CloseUIByLayer(layer.LayerId);
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
        public int Layer;
        public string Path;
        public bool IsMultiple;
        public bool IsCoexist;
    }
}