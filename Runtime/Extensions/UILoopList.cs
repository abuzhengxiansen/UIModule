using System;
using System.Collections;
using System.Collections.Generic;
using GamePlay;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    public class UILoopList : UIBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        private enum Direction
        {
            Horizontal,
            Vertical
        }
        
        #region public properties
        
        [SerializeField, Tooltip("元素预制体，可以通过Init方法动态传入")]
        private GameObject template;
        // 内容容器
        [SerializeField, Tooltip("元素容器，必须设置")]
        private RectTransform content;
        
        // 滚动方向
        [SerializeField]
        private Direction direction = Direction.Vertical;
        // 反转排布
        [SerializeField, Tooltip("是否反方向排布，自上向下与自右向左为正方向")]
        private bool reverseDirection;
        // 磁吸模式
        [SerializeField, Tooltip("是否启用磁吸模式，启用后滚动停止时会自动对齐到最近的元素位置")]
        private bool snapMode;
        
        // 布局属性
        [SerializeField]
        private RectOffset padding;
        [SerializeField]
        private float spacing;
        
        // 滚轮敏感度
        [SerializeField, Tooltip("对滚轮和触控板滚动事件的敏感性，值越大越敏感")]
        private float scrollSensitivity = 1f;
        // 惯性衰减速度
        [SerializeField, Tooltip("惯性衰减速度，值越大衰减越快，0为无惯性效果")]
        private float decelerationRate = 0.135f;
        // 弹性效果
        [SerializeField, Tooltip("回弹时间，值越小回弹越快，0为无弹性效果")]
        private float elasticity = 0.1f;
        
        #endregion
        
        #region privte fields
        
        private Type _itemType;
        private RectTransform _rectTransform;
        private RectTransform _viewport;
        private float[] _usePadding;
        private float _useSpacing;
        private float _itemSizeSpacing;
        
        // 列表管理
        private int _totalCount;
        private readonly List<BaseWidget> _itemPool = new ();
        private readonly Dictionary<int, BaseWidget> _activeItems = new ();
        private readonly Dictionary<int, float> _itemPositions = new ();
        
        // 拖拽相关
        private bool _isDragging;
        private float _dragStartPos;
        private float _contentStartPos;
        private float _velocity;
        private float _prevPosition;
        
        // 拖拽回调
        private Action<PointerEventData> _onBeginDragCallback;
        private Action<PointerEventData> _onDragCallback;
        private Action<PointerEventData> _onEndDragCallback;
        
        // 平滑移动
        private Coroutine _moveCoroutine;
        
        // 惯性
        private bool _isInertia;
        // 弹性回弹
        private bool _isElasticity;
        // 磁吸对齐
        private bool _isSnapping;
        private int _snapTargetIndex = -1;
        
        #endregion

        protected override void Awake()
        {
            base.Awake();
            _rectTransform = GetComponent<RectTransform>();
            _viewport = _rectTransform;
        }

        protected override void Start()
        {
            base.Start();
            if (content != null)
            {
                _prevPosition = direction == Direction.Vertical ? content.anchoredPosition.y : content.anchoredPosition.x;
            }
        }

        private void Update()
        {
            if (!content) return;

            if (!_isDragging)
            {
                // 处理惯性滚动
                if (_isInertia)
                {
                    UpdateInertia();
                }
                // 处理弹性回弹
                else if (_isElasticity)
                {
                    UpdateElasticBounce();
                }
                else if (_isSnapping)
                {
                    UpdateSnapping();
                }
            }
        }

        /// <summary>
        /// 初始化列表
        /// </summary>
        /// <param name="prefab">元素预制体</param>
        /// <typeparam name="T">元素组件</typeparam>
        public void Init<T>(GameObject prefab = null) where T : BaseWidget, ILoopItem
        {
            if (prefab)
            {
                template = prefab;
            }
            
            _itemType = typeof(T);

            InitUseData();

            // 清理现有项
            ClearItems();
            
            // 设置content的布局
            AdaptContentTransform();
        }

        /// <summary>
        /// 刷新列表，触发onRefresh
        /// </summary>
        /// <param name="num">更新当前的列表数量</param>
        /// <param name="isResetPos">是否重置列表到起始位置</param>
        public void Refresh(int num = -1, bool isResetPos = false)
        {
            if (num >= 0)
            {
                _totalCount = num;
            }
            
            // 如果需要重置位置，先重置
            if (isResetPos)
            {
                ResetPosition();
            }
            
            // 更新所有item的位置和content的大小
            UpdateItemPositions();
            
            // 更新可见的items
            UpdateVisibleItems();
        }

        /// <summary>
        /// 刷新某个序号的元素，触发onRefresh
        /// </summary>
        /// <param name="index">更新元素的序号</param>
        public void RefreshItem(int index)
        {
            if (index < 0 || index >= _totalCount) return;
            
            if (_activeItems.TryGetValue(index, out var item) && item is ILoopItem loopItem)
            {
                loopItem.RefreshItem(index);
            }
        }
        
        /// <summary>
        /// 移动至指定序号元素的位置
        /// </summary>
        /// <param name="index">移动指定的序号</param>
        /// <param name="time">移动的时间，如果是0则直接刷新位置</param>
        public void MoveToItem(int index, float time = 0f)
        {
            if (index < 0 || index >= _totalCount) return;
            
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }
            
            // 获取目标item的位置
            var targetItemPos = GetItemPosition(index);
            var targetAnchoredPos = -targetItemPos;
            
            // 应用边界限制
            targetAnchoredPos = ClampContentPosition(targetAnchoredPos);
            
            if (time <= 0)
            {
                content.anchoredPosition = direction == Direction.Vertical ?
                    new Vector2(content.anchoredPosition.x, targetAnchoredPos) :
                    new Vector2(targetAnchoredPos, content.anchoredPosition.y);
                _velocity = 0;
                _prevPosition = targetAnchoredPos;
                UpdateVisibleItems();
            }
            else
            {
                _moveCoroutine = StartCoroutine(SmoothMoveCoroutine(targetAnchoredPos, time));
            }
        }

        /// <summary>
        /// 判断指定序号的元素是否在可视范围内
        /// </summary>
        /// <param name="index">元素序号</param>
        /// <returns>是否在可视范围内</returns>
        public bool IsItemVisible(int index)
        {
            if (index < 0 || index >= _totalCount) return false;
            return _activeItems.ContainsKey(index);
        }
        
        #region 拖拽事件回调设置
        
        /// <summary>
        /// 设置开始拖拽回调
        /// </summary>
        public void SetOnBeginDragCallback(Action<PointerEventData> callback)
        {
            _onBeginDragCallback = callback;
        }
        
        /// <summary>
        /// 设置拖拽中回调
        /// </summary>
        public void SetOnDragCallback(Action<PointerEventData> callback)
        {
            _onDragCallback = callback;
        }
        
        /// <summary>
        /// 设置结束拖拽回调
        /// </summary>
        public void SetOnEndDragCallback(Action<PointerEventData> callback)
        {
            _onEndDragCallback = callback;
        }
        
        #endregion
        
        #region 拖拽事件实现
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (content == null) return;
            
            if (!IsValidDrag(eventData)) return;

            _isInertia = false;
            _isElasticity = false;
            _isSnapping = false;
            _isDragging = true;
            _dragStartPos = direction == Direction.Vertical ? eventData.position.y : eventData.position.x;
            _contentStartPos = direction == Direction.Vertical ? content.anchoredPosition.y : content.anchoredPosition.x;
            _velocity = 0;
            
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }
            
            _onBeginDragCallback?.Invoke(eventData);
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || content == null) return;
            
            var pos = direction == Direction.Vertical ? eventData.position.y : eventData.position.x;
            var offset = pos - _dragStartPos;
            
            // 应用弹性效果
            var targetPos = ApplyElasticity(_contentStartPos + offset);
            
            content.anchoredPosition = direction == Direction.Vertical ?
                new Vector2(content.anchoredPosition.x, targetPos) :
                new Vector2(targetPos, content.anchoredPosition.y);
            
            // 计算速度
            var deltaPosition = targetPos - _prevPosition;
            _velocity = deltaPosition / Time.unscaledDeltaTime;
            _prevPosition = targetPos;
            
            UpdateVisibleItems();
            
            _onDragCallback?.Invoke(eventData);
        }
        
        /// <summary>
        /// 应用弹性效果，拖拽超出边界时减少移动距离
        /// </summary>
        private float ApplyElasticity(float position)
        {
            if (content == null || _viewport == null || _totalCount == 0) return position;
            
            var contentSize = GetContentSize();
            var viewportSize = GetViewportSize();
            
            // 如果内容小于等于视口，不允许移动
            if (contentSize <= viewportSize)
            {
                return 0f;
            }
            
            var maxScroll = contentSize - viewportSize;
            
            // 检查是否超出边界，计算超出的原始距离
            var overScroll = 0f;
            var validBound = 0f;
            
            if (reverseDirection)
            {
                if (position > 0)
                {
                    overScroll = position;
                    validBound = 0;
                }
                else if (position < -maxScroll)
                {
                    overScroll = position - (-maxScroll);
                    validBound = -maxScroll;
                }
                else
                {
                    return position;
                }
            }
            else
            {
                if (position < 0)
                {
                    overScroll = position;
                    validBound = 0;
                }
                else if (position > maxScroll)
                {
                    overScroll = position - maxScroll;
                    validBound = maxScroll;
                }
                else
                {
                    return position;
                }
            }
            
            // 如果elasticity == 0f则无法超越边界
            if (elasticity <= 0f)
            {
                return validBound;
            }
            
            var dampedScroll = viewportSize * (1f - 1f / (1f + Mathf.Abs(overScroll) / viewportSize));
            dampedScroll *= Mathf.Sign(overScroll);
            
            return validBound + dampedScroll;
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;
            
            // 如果开启了弹性且超出边界，标记需要回弹
            if (elasticity > 0 && IsOutOfBounds())
            {
                _isElasticity = true;
            }

            if (decelerationRate > 0f && _velocity != 0f)
            {
                _isInertia = true;
            }
            else if (snapMode && !_isElasticity && _totalCount > 0)
            {
                // 无惯性时且不超边界，直接启动磁吸对齐
                StartSnapping();
            }
            
            _onEndDragCallback?.Invoke(eventData);
        }
        
        public void OnScroll(PointerEventData eventData)
        {
            if (content == null || _totalCount == 0) return;
            
            // 获取滚动增量
            var delta = Mathf.Abs(eventData.scrollDelta.y) > Mathf.Abs(eventData.scrollDelta.x) ? eventData.scrollDelta.y : eventData.scrollDelta.x;
            
            // 如果没有滚动增量，忽略
            if (Mathf.Abs(delta) < 0.01f) return;
            
            // 应用敏感度并反转方向（滚轮向上滚动时，content应该向下移动） 乘以20是为了让滚动更明显
            delta *= - scrollSensitivity * 20f;
            
            _isInertia = false;
            _isElasticity = false;
            _isSnapping = false;
            _isDragging = false;
            _velocity = 0f;
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }
            
            // 计算新位置
            var currentPos = direction == Direction.Vertical ? content.anchoredPosition.y : content.anchoredPosition.x;
            var targetPos = currentPos + delta;
            
            // 应用弹性效果
            targetPos = ApplyElasticity(targetPos);
            
            // 更新位置
            content.anchoredPosition = direction == Direction.Vertical ?
                new Vector2(content.anchoredPosition.x, targetPos) :
                new Vector2(targetPos, content.anchoredPosition.y);
            
            _prevPosition = targetPos;
            UpdateVisibleItems();
            
            // 如果超出边界且有弹性，启动回弹
            if (elasticity > 0 && IsOutOfBounds())
            {
                _isElasticity = true;
            }
            // 如果开启了磁吸，启动磁吸对齐
            else if (snapMode && !IsOutOfBounds())
            {
                StartSnapping();
            }
        }
        
        #endregion
        
        #region 私有方法

        public void InitUseData()
        {
            _usePadding = new float[2];
            if (direction == Direction.Vertical)
            {
                _usePadding[0] = reverseDirection ? padding.bottom : -padding.top;
                _usePadding[1] = reverseDirection ? padding.top : -padding.bottom;
            }
            else
            {
                _usePadding[0] = reverseDirection ? padding.left : -padding.right;
                _usePadding[1] = reverseDirection ? padding.right : -padding.left;
            }
            
            _useSpacing = reverseDirection ? spacing : -spacing;
            
            // 只有当template存在时才计算item大小
            if (template)
            {
                var rect = template.GetComponent<RectTransform>();
                var size = direction == Direction.Vertical ? rect.sizeDelta.y : rect.sizeDelta.x;
                _itemSizeSpacing = reverseDirection ? size : -size;
            }
        }
        
        /// <summary>
        /// 根据direction和reverseDirection自动设置content的anchor和pivot
        /// </summary>
        public void AdaptContentTransform()
        {
            if (!content) return;
            
            // 根据方向和反转设置anchor、pivot和offset
            if (direction == Direction.Vertical)
            {
                if (reverseDirection)
                {
                    // 垂直反转：从下往上，anchor在底部
                    content.anchorMin = new Vector2(0, 0);
                    content.anchorMax = new Vector2(1, 0);
                    content.pivot = new Vector2(0.5f, 0);
                }
                else
                {
                    // 垂直正常：从上往下，anchor在顶部
                    content.anchorMin = new Vector2(0, 1);
                    content.anchorMax = new Vector2(1, 1);
                    content.pivot = new Vector2(0.5f, 1);
                }

                // 设置offsetMin和offsetMax，使content横向撑满
                content.offsetMin = new Vector2(0, content.offsetMin.y);
                content.offsetMax = new Vector2(0, content.offsetMax.y);

                // 垂直滚动时，宽度自动跟随viewport，只设置高度
                var currentHeight = content.sizeDelta.y;
                content.sizeDelta = new Vector2(0, currentHeight);
            }
            else
            {
                if (reverseDirection)
                {
                    // 水平反转：从左往右，anchor在左边
                    content.anchorMin = new Vector2(0, 0);
                    content.anchorMax = new Vector2(0, 1);
                    content.pivot = new Vector2(0, 0.5f);
                }
                else
                {
                    // 水平正常：从右往左，anchor在右边
                    content.anchorMin = new Vector2(1, 0);
                    content.anchorMax = new Vector2(1, 1);
                    content.pivot = new Vector2(1, 0.5f);
                }

                // 设置offsetMin和offsetMax，使content纵向撑满
                content.offsetMin = new Vector2(content.offsetMin.x, 0);
                content.offsetMax = new Vector2(content.offsetMax.x, 0);

                // 水平滚动时，高度自动跟随viewport，只设置宽度
                if (_viewport)
                {
                    // 保持当前宽度，高度由anchor自动确定
                    var currentWidth = content.sizeDelta.x;
                    content.sizeDelta = new Vector2(currentWidth, 0);
                }
            }
        }
        
        private bool IsValidDrag(PointerEventData eventData)
        {
            var delta = eventData.position - eventData.pressPosition;
            return direction == Direction.Vertical ? Mathf.Abs(delta.y) > 1f : Mathf.Abs(delta.x) > 1f;
        }
        
        private void UpdateInertia()
        {
            // 应用惯性
            var newPosition = direction == Direction.Vertical ? content.anchoredPosition.y : content.anchoredPosition.x;
            newPosition += _velocity * Time.unscaledDeltaTime;
            
            // 检查是否会超出边界
            var willBeOutOfBounds = false;
            if (elasticity > 0)
            {
                // 如果有弹性，允许超出边界，但需要检查是否已经超出
                var diff = Mathf.Abs(newPosition - ClampContentPosition(newPosition));
                var radio = diff > 0f ? 1f - Mathf.Clamp01(diff / (GetViewportSize() * 0.5f)) : 1f;
                _velocity *= radio;
                
                if (diff > 0.01f)
                {
                    willBeOutOfBounds = true;
                }
            }
            else
            {
                // 没有弹性，直接限制在边界内
                newPosition = ClampContentPosition(newPosition);
            }
            
            content.anchoredPosition = direction == Direction.Vertical ?
                new Vector2(content.anchoredPosition.x, newPosition) :
                new Vector2(newPosition, content.anchoredPosition.y);
            
            // 如果开启了磁吸模式且不会超出边界，开始向目标平滑过渡
            if (snapMode && _totalCount > 0 && !willBeOutOfBounds && Mathf.Abs(_velocity) < 500f)
            {
                // 找到最近的元素
                var nearestIndex = GetNearestItemIndex();
                if (nearestIndex >= 0)
                {
                    // 计算目标位置
                    var targetItemPos = GetItemPosition(nearestIndex);
                    var targetPosition = reverseDirection ? targetItemPos : -targetItemPos;
                    
                    targetPosition = ClampContentPosition(targetPosition);
                    
                    // 计算到目标的距离和方向
                    var distanceToTarget = targetPosition - newPosition;
                    
                    // 如果距离较近且速度方向与目标一致或速度较小，开始平滑过渡
                    var isMovingTowardsTarget = Mathf.Approximately(Mathf.Sign(distanceToTarget), Mathf.Sign(_velocity)) || Mathf.Abs(_velocity) < 100f;
                    
                    if (isMovingTowardsTarget && Mathf.Abs(distanceToTarget) < GetViewportSize() * 0.5f)
                    {
                        // 使用插值让速度逐渐指向目标
                        var targetVelocity = distanceToTarget * 5f; // 调整速度朝向目标
                        _velocity = Mathf.Lerp(_velocity, targetVelocity, Time.unscaledDeltaTime * 3f);
                        
                        // 如果已经很接近目标，直接切换到磁吸模式
                        if (Mathf.Abs(distanceToTarget) < 1f)
                        {
                            _isInertia = false;
                            _isSnapping = true;
                            _snapTargetIndex = nearestIndex;
                            _prevPosition = newPosition;
                            UpdateVisibleItems();
                            return;
                        }
                    }
                }
            }
            
            // 衰减速度
            _velocity *= Mathf.Pow(decelerationRate, Time.unscaledDeltaTime);
            
            // 停止条件：速度很小
            if (Mathf.Abs(_velocity) < 1f)
            {
                _isInertia = false;
                _velocity = 0f;
                
                // 如果停止时超出边界且有弹性，触发回弹
                if (elasticity > 0 && IsOutOfBounds())
                {
                    _isElasticity = true;
                }
                // 如果开启了磁吸模式且没有超出边界，启动磁吸对齐
                else if (snapMode && _totalCount > 0 && !IsOutOfBounds())
                {
                    StartSnapping();
                }
            }
            
            _prevPosition = newPosition;
            UpdateVisibleItems();
        }
        
        /// <summary>
        /// 限制content位置在合理范围内
        /// </summary>
        private float ClampContentPosition(float position)
        {
            if (!content || !_viewport || _totalCount == 0) return position;
            
            var contentSize = GetContentSize();
            var viewportSize = GetViewportSize();
            
            // 如果内容小于等于视口，不需要滚动，保持在起始位置
            if (contentSize <= viewportSize) return 0f;
            
            // 计算最大可滚动距离
            var maxScroll = contentSize - viewportSize;
            
            if (reverseDirection)
            {
                // -maxScroll 到 0
                position = Mathf.Clamp(position, -maxScroll, 0);
            }
            else
            {
                // 0 到 maxScroll
                position = Mathf.Clamp(position, 0, maxScroll);
            }
            
            return position;
        }
        
        /// <summary>
        /// 检查是否超出边界
        /// </summary>
        private bool IsOutOfBounds()
        {
            if (!content || !_viewport || _totalCount == 0) return false;
            
            var contentSize = GetContentSize();
            var viewportSize = GetViewportSize();
            
            // 如果内容小于等于视口，任何非零位置都算超出边界
            if (contentSize <= viewportSize)
            {
                var currentPos = content.anchoredPosition;
                if (direction == Direction.Vertical)
                {
                    return Mathf.Abs(currentPos.y) > 0.01f;
                }
                else
                {
                    return Mathf.Abs(currentPos.x) > 0.01f;
                }
            }
            
            var maxScroll = contentSize - viewportSize;
            var pos = direction == Direction.Vertical ? content.anchoredPosition.y : content.anchoredPosition.x;
            
            if (direction == Direction.Vertical)
            {
                if (reverseDirection)
                {
                    // -maxScroll 到 0
                    return pos > 0.01f || pos < -maxScroll - 0.01f;
                }
                else
                {
                    // 0 到 maxScroll
                    return pos < -0.01f || pos > maxScroll + 0.01f;
                }
            }
            else
            {
                if (reverseDirection)
                {
                    // -maxScroll 到 0
                    return pos > 0.01f || pos < -maxScroll - 0.01f;
                }
                else
                {
                    // 0 到 maxScroll
                    return pos < -0.01f || pos > maxScroll + 0.01f;
                }
            }
        }
        
        /// <summary>
        /// 弹性回弹更新
        /// </summary>
        private void UpdateElasticBounce()
        {
            if (!content || !_viewport || _totalCount == 0)
            {
                _isElasticity = false;
                return;
            }
            
            var currentPosition = direction == Direction.Vertical ? content.anchoredPosition.y : content.anchoredPosition.x;
            var targetPosition = ClampContentPosition(currentPosition);
            
            // 计算到目标位置的距离
            var distance = Mathf.Abs(targetPosition - currentPosition);
            
            // 如果已经很接近目标位置，直接设置到目标位置
            if (distance < 0.1f)
            {
                content.anchoredPosition = direction == Direction.Vertical ?
                    new Vector2(content.anchoredPosition.x, targetPosition) :
                    new Vector2(targetPosition, content.anchoredPosition.y);
                _isElasticity = false;
                _prevPosition = targetPosition;
                UpdateVisibleItems();
                return;
            }
            
            // 使用弹性系数进行平滑移动
            // elasticity越大，回弹越慢；elasticity越小，回弹越快
            var smoothTime = Mathf.Max(0.1f, elasticity);
            
            // 使用SmoothDamp实现平滑回弹
            var newPos = Mathf.SmoothDamp(currentPosition, targetPosition, ref _velocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            
            content.anchoredPosition = direction == Direction.Vertical ?
                new Vector2(content.anchoredPosition.x, newPos) :
                new Vector2(newPos, content.anchoredPosition.y);
            _prevPosition = newPos;
            UpdateVisibleItems();
        }
        
        /// <summary>
        /// 启动磁吸对齐
        /// </summary>
        private void StartSnapping()
        {
            if (!snapMode || _totalCount == 0) return;
            
            // 找到最近的元素
            _snapTargetIndex = GetNearestItemIndex();
            if (_snapTargetIndex >= 0)
            {
                _isSnapping = true;
            }
        }
        
        /// <summary>
        /// 磁吸对齐更新
        /// </summary>
        private void UpdateSnapping()
        {
            if (!content || _snapTargetIndex < 0 || _snapTargetIndex >= _totalCount)
            {
                _isSnapping = false;
                return;
            }
            
            // 获取目标位置
            var targetItemPos = GetItemPosition(_snapTargetIndex);
            var targetPosition = reverseDirection ? targetItemPos : -targetItemPos;
            
            // 应用边界限制
            targetPosition = ClampContentPosition(targetPosition);
            
            var currentPosition = direction == Direction.Vertical ? content.anchoredPosition.y : content.anchoredPosition.x;
            
            // 计算到目标位置的距离
            var distance = Mathf.Abs(targetPosition - currentPosition);
            
            // 如果已经很接近目标位置，直接设置到目标位置
            if (distance < 0.1f)
            {
                content.anchoredPosition = direction == Direction.Vertical ?
                    new Vector2(content.anchoredPosition.x, targetPosition) :
                    new Vector2(targetPosition, content.anchoredPosition.y);
                _isSnapping = false;
                _velocity = 0f;
                _prevPosition = targetPosition;
                UpdateVisibleItems();
                return;
            }
            
            // 使用SmoothDamp实现平滑对齐，利用现有的速度进行平滑过渡
            var smoothTime = 0.2f; // 对齐时间固定为0.2秒
            var newPos = Mathf.SmoothDamp(currentPosition, targetPosition, ref _velocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            
            content.anchoredPosition = direction == Direction.Vertical ?
                new Vector2(content.anchoredPosition.x, newPos) :
                new Vector2(newPos, content.anchoredPosition.y);
            _prevPosition = newPos;
            UpdateVisibleItems();
        }
        
        /// <summary>
        /// 获取距离视口起始边最近的元素索引
        /// </summary>
        private int GetNearestItemIndex()
        {
            if (_totalCount == 0 || !content || !_viewport) return -1;
            
            var currentPosition = direction == Direction.Vertical ? content.anchoredPosition.y : content.anchoredPosition.x;
            
            // 计算视口起始边对应的内容位置
            var viewportStartPos = reverseDirection ? currentPosition : -currentPosition;
            
            // 找到最接近视口起始边的元素
            var nearestIndex = 0;
            var minDistance = float.MaxValue;
            
            for (var i = 0; i < _totalCount; i++)
            {
                var itemPos = GetItemPosition(i);
                var distance = Mathf.Abs(itemPos - viewportStartPos);
                
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestIndex = i;
                }
            }
            
            return nearestIndex;
        }
        
        private float GetContentSize()
        {
            if (content == null) return 0f;
            return direction == Direction.Vertical ? content.sizeDelta.y : content.sizeDelta.x;
        }
        
        private float GetViewportSize()
        {
            if (_viewport == null) return 0f;
            return direction == Direction.Vertical ? _viewport.rect.height : _viewport.rect.width;
        }
        
        private void ResetPosition()
        {
            if (content == null) return;
            
            _velocity = 0;
            
            if (direction == Direction.Vertical)
            {
                // 垂直方向：无论是否反转，初始位置都是0（anchor已经设置好了）
                content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0);
            }
            else
            {
                // 水平方向：无论是否反转，初始位置都是0（anchor已经设置好了）
                content.anchoredPosition = new Vector2(0, content.anchoredPosition.y);
            }
            
            _prevPosition = direction == Direction.Vertical ? content.anchoredPosition.y : content.anchoredPosition.x;
        }
        
        private void UpdateItemPositions()
        {
            _itemPositions.Clear();
            
            var currentPos = _usePadding[0];
            
            for (var i = 0; i < _totalCount; i++)
            {
                _itemPositions[i] = currentPos;
                currentPos += _itemSizeSpacing + _useSpacing;
            }
            
            // 更新content大小
            if (content)
            {
                // 计算总大小：最后一个元素的位置 + 最后一个元素的大小 + 结束padding
                // 确保最小大小不小于viewport
                var totalSize = Mathf.Max(Math.Abs(currentPos - _useSpacing + _usePadding[1]), GetViewportSize());
                
                var contentSize = content.sizeDelta;
                
                if (direction == Direction.Vertical)
                {
                    contentSize.y = totalSize;
                }
                else
                {
                    contentSize.x = totalSize;
                }
                
                content.sizeDelta = contentSize;
            }
        }
        
        private void UpdateVisibleItems()
        {
            if (!content || !_viewport) return;
            
            // 计算可视范围
            var(viewportMin, viewportMax) = GetViewportBox();
            
            // 找出需要显示的项
            var visibleIndices = new HashSet<int>();
            
            for (var i = 0; i < _totalCount; i++)
            {
                if (IsItemInViewport(i, viewportMin, viewportMax))
                {
                    visibleIndices.Add(i);
                }
            }
            
            // 回收不可见的项
            var toRemove = new List<int>();
            foreach (var kvp in _activeItems)
            {
                if (!visibleIndices.Contains(kvp.Key))
                {
                    toRemove.Add(kvp.Key);
                }
            }
            
            foreach (var index in toRemove)
            {
                RecycleItem(index);
            }
            
            // 创建或更新可见项
            foreach (var index in visibleIndices)
            {
                if (!_activeItems.ContainsKey(index))
                {
                    CreateOrReuseItem(index);
                }
            }
        }
        
        private bool IsItemInViewport(int index, float viewportMin, float viewportMax)
        {
            if (!_itemPositions.TryGetValue(index, out var itemPos)) return false;

            var itemMax = itemPos + _itemSizeSpacing;
            
            // 添加一些缓冲区域，提前加载即将进入视野的元素
            var buffer = _itemSizeSpacing;
            
            var vMin = viewportMin + buffer;
            var vMax = viewportMax - buffer;
            return itemMax >= vMin && itemPos <= vMax;
        }
        
        private (float, float) GetViewportBox()
        {
            if (!content || !_viewport) return (0f, 0f);
            
            // 获取viewport的大小
            var viewportSize = GetViewportSize();
            var contentPos = content.anchoredPosition;
            
            if (direction == Direction.Vertical)
            {
                return reverseDirection ?
                    // 从下往上：viewport底部对应contentPos.y
                    (contentPos.y, contentPos.y + viewportSize) :
                    // 从上往下：viewport顶部对应-contentPos.y
                    (-contentPos.y - viewportSize, -contentPos.y);
            }

            return reverseDirection ?
                // 从左往右：viewport左侧对应contentPos.x
                (contentPos.x, contentPos.x + viewportSize) :
                // 从右往左：viewport右侧对应-contentPos.x - viewportSize
                (-contentPos.x - viewportSize, -contentPos.x);
        }
        
        private void CreateOrReuseItem(int index)
        {
            var widget = GetItemFromPool();
            
            if (widget == null)
            {
                // 创建新项
                var go = Instantiate(template, content);
                
                if (go.transform.parent != content)
                {
                    go.transform.SetParent(content, false);
                }

                widget = Activator.CreateInstance(_itemType) as BaseWidget;
                
                widget.SetWidgetData("loopItem" + index, go);
                widget.Create();
                widget.Status = UIStatus.Showing;
                
                AdaptItemTransform(widget.Rect);
                
                // 调用创建回调
                if (widget is ILoopItem loopItem)
                {
                    loopItem.InitItem(index);
                }
            }
            
            // 设置位置
            SetItemPosition(widget.Rect, index);
            
            // 添加到活动列表
            _activeItems[index] = widget;
            widget.Go.SetActive(true);
            
            // 调用刷新回调
            if (widget is ILoopItem loopItemRefresh)
            {
                loopItemRefresh.RefreshItem(index);
            }
        }
        
        private void AdaptItemTransform(RectTransform rect)
        {
            if (direction == Direction.Vertical)
            {
                if (reverseDirection)
                {
                    // 从下往上：元素anchor在底部，pivot在底部
                    rect.anchorMin = new Vector2(0.5f, 0);
                    rect.anchorMax = new Vector2(0.5f, 0);
                    rect.pivot = new Vector2(0.5f, 0);
                }
                else
                {
                    // 从上往下：元素anchor在顶部，pivot在顶部
                    rect.anchorMin = new Vector2(0.5f, 1);
                    rect.anchorMax = new Vector2(0.5f, 1);
                    rect.pivot = new Vector2(0.5f, 1);
                }
            }
            else
            {
                if (reverseDirection)
                {
                    // 从左往右：元素anchor在左边，pivot在左边
                    rect.anchorMin = new Vector2(0, 0.5f);
                    rect.anchorMax = new Vector2(0, 0.5f);
                    rect.pivot = new Vector2(0, 0.5f);
                }
                else
                {
                    // 从右往左：元素anchor在右边，pivot在右边
                    rect.anchorMin = new Vector2(1, 0.5f);
                    rect.anchorMax = new Vector2(1, 0.5f);
                    rect.pivot = new Vector2(1, 0.5f);
                }
            }
        }
        
        private void SetItemPosition(RectTransform rect, int index)
        {
            if (!_itemPositions.TryGetValue(index, out var pos)) return;
            
            var anchoredPos = rect.anchoredPosition;
            
            if (direction == Direction.Vertical)
            {
                anchoredPos.y = pos;
                anchoredPos.x = 0;
            }
            else
            {
                anchoredPos.x = pos;
                anchoredPos.y = 0;
            }
            
            rect.anchoredPosition = anchoredPos;
        }
        
        private void RecycleItem(int index)
        {
            if (!_activeItems.TryGetValue(index, out var item)) return;
            
            item.Go.SetActive(false);
            _itemPool.Add(item);
            _activeItems.Remove(index);
        }
        
        private BaseWidget GetItemFromPool()
        {
            if (_itemPool.Count <= 0) return null;
            
            var item = _itemPool[0];
            _itemPool.RemoveAt(0);
            return item;
        }
        
        private float GetItemPosition(int index)
        {
            return _itemPositions.GetValueOrDefault(index, 0f);
        }
        
        private void ClearItems()
        {
            foreach (var (_, kvp) in _activeItems)
            {
                kvp?.Dispose();
            }
            _activeItems.Clear();
            
            foreach (var item in _itemPool)
            {
                item.Dispose();
            }
            _itemPool.Clear();
            
            _itemPositions.Clear();
        }
        
        private IEnumerator SmoothMoveCoroutine(float targetPos, float duration)
        {
            var elapsed = 0f;
            var startPos = direction == Direction.Vertical ? content.anchoredPosition.y : content.anchoredPosition.x;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                
                // 使用平滑曲线
                t = Mathf.SmoothStep(0f, 1f, t);
                
                var value = Mathf.Lerp(startPos, targetPos, t);
                content.anchoredPosition = direction == Direction.Vertical ?
                    new Vector2(content.anchoredPosition.x, value) :
                    new Vector2(value, content.anchoredPosition.y);
                UpdateVisibleItems();
                
                yield return null;
            }
            
            content.anchoredPosition = direction == Direction.Vertical ?
                new Vector2(content.anchoredPosition.x, targetPos) :
                new Vector2(targetPos, content.anchoredPosition.y);
            _velocity = 0;
            UpdateVisibleItems();
            
            _moveCoroutine = null;
        }
        
        #endregion
        
        #region 公开方法
        
        /// <summary>
        /// 获取当前列表总数
        /// </summary>
        public int GetTotalCount()
        {
            return _totalCount;
        }
        
        /// <summary>
        /// 清空列表
        /// </summary>
        public void Clear()
        {
            _totalCount = 0;
            ClearItems();
            
            if (content != null)
            {
                content.anchoredPosition = Vector2.zero;
            }
        }
        
        #endregion
    }
}