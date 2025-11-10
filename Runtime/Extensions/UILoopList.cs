using System;
using System.Collections.Generic;
using System.Linq;
using GamePlay;
using LiteQuark.Runtime;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    public class UILoopList : ScrollRect
    {
        public enum DirectionType
        {
            Horizontal,
            Vertical
        }
        
        private class ItemData
        {
            public int Index;
            public BaseWidget Widget;
        }
        
        // 布局属性
        public RectOffset padding;
        public float spacing;
        
        [SerializeField] private float itemSize;
        [SerializeField] private DirectionType direction = DirectionType.Vertical;
        [SerializeField] private bool reverseArrangement;
        [SerializeField] private bool isDynamicSize;
        
        private bool _isInit;
    
        private RectTransform _contentRect;
        private readonly List<ItemData> _itemList = new ();
        private readonly List<float> _itemPoints = new ();
        private float _viewSize;
        
        private bool _isDragging;
        private int _appointCount;
        private int _minBorder = -1;
        private int _maxBorder = -1;

        private float ContentPos => (reverseArrangement ? -1 : 1) * (direction == DirectionType.Vertical ? _contentRect.anchoredPosition.y : _contentRect.anchoredPosition.x);

        private float ContentSize => direction == DirectionType.Vertical ? _contentRect.sizeDelta.y : _contentRect.sizeDelta.x;
        
        // 两种刷新模式
        private Action<int, BaseWidget> _itemRefreshAction;
        private Func<int, Transform, BaseWidget> _itemCreateFunc;
        
        // 事件系统
        private Action<UILoopList> _onDragBegin;
        private Action<UILoopList> _onDragEnd;
        private Action<UILoopList> _onDragging;
        
        protected override void OnEnable() {
            base.OnEnable();
            onValueChanged.AddListener(OnScroll); // 只在滚动时触发
        }
        
        protected override void OnDisable() {
            base.OnDisable();
            onValueChanged.RemoveListener(OnScroll);
        }

        private void RefreshBaseInfo()
        {
            var viewSize = transform.GetComponent<RectTransform>().sizeDelta;
            _viewSize = direction == DirectionType.Vertical ? viewSize.y : viewSize.x;
        }

        private void OnScroll(Vector2 pos) {
            if (!_isInit) return;
            AutoRefreshItems();
        }
    
        public void Init(Action<int, BaseWidget> refreshAction, Func<int, Transform, BaseWidget> createFunc)
        {
            if (_isInit)
            {
                return;
            }
            _isInit = true;
            
            _itemRefreshAction = refreshAction;
            _itemCreateFunc = createFunc;
            
            RefreshBaseInfo();
            
            _contentRect = content.GetComponent<RectTransform>();
            
            var count = Mathf.CeilToInt(_viewSize / itemSize) + 1;
            
            for(var i = 0; i < count; i++)
            {
                CreateItem(i);
            }
            
            InitContent();
        }
    
        private void CreateItem(int index)
        {
            var widget = _itemCreateFunc(index, content.transform);
    
            var pos = new Vector2(0.5f, reverseArrangement ? 0f : 1f);
            if (direction == DirectionType.Vertical)
            {
                pos = new Vector2(0.5f, reverseArrangement ? 0f : 1f);
            }
            else if (direction == DirectionType.Horizontal)
            {
                pos = new Vector2(reverseArrangement ? 1f : 0f, 0.5f);
            }
            widget.Rect.anchorMin = pos;
            widget.Rect.anchorMax = pos;
            widget.Rect.pivot = pos;
            
            widget.Rect.anchoredPosition = Vector2.one * 999999f;
            widget.Go.SetActive(false);
            
            _itemList.Add(new ItemData{Index = index, Widget = widget});
        }
    
        private void InitContent()
        {
            var contentRect = content.GetComponent<RectTransform>();
            if (direction == DirectionType.Vertical)
            {
                contentRect.anchorMin = reverseArrangement ? Vector2.zero : new Vector2(0, 1);
                contentRect.anchorMax = reverseArrangement ? new Vector2(1, 0) : Vector2.one;
                contentRect.pivot = new Vector2(0.5f, reverseArrangement ? 0 : 1);
            }
            else if (direction == DirectionType.Horizontal)
            {
                contentRect.anchorMin = reverseArrangement ? new Vector2(1, 0) : Vector2.zero;
                contentRect.anchorMax = reverseArrangement ? Vector2.one : new Vector2(0, 1);
                contentRect.pivot = new Vector2(reverseArrangement ? 1 : 0, 0.5f);
            }
        }
    
        private void RefreshContentSize()
        {
            if (direction == DirectionType.Vertical)
            {
                _contentRect.sizeDelta = new Vector2(0, itemSize * _appointCount + spacing * (_appointCount - 1) + padding.vertical);
            }
            else if (direction == DirectionType.Horizontal)
            {
                _contentRect.sizeDelta = new Vector2(itemSize * _appointCount + spacing * (_appointCount - 1) + padding.horizontal, 0);
            }
        }
        
        private void RefreshItemPoints()
        {
            var curPoint = direction switch
            {
                DirectionType.Vertical => reverseArrangement ? padding.bottom : -padding.top,
                DirectionType.Horizontal => reverseArrangement ? -padding.right : padding.left,
                _ => 0f
            };
            var itemSpace = itemSize + spacing;
            _itemPoints.Clear();
            for (var i = 0; i < _itemList.Count; i++)
            {
                _itemPoints.Add(curPoint);
                curPoint += itemSpace;
            }
        }
        
        public void Refresh(int count, bool isReset)
        {
            if (!_isInit)
            {
                return;
            }
            if (_itemRefreshAction == null)
            {
                return;
            }
    
            if (count != _appointCount)
            {
                _appointCount = count;
                RefreshContentSize();
                RefreshItemPoints();
                for (var i = 0; i < _itemList.Count; i++)
                {
                    _itemList[i].Widget.Go.SetActive(i < _appointCount);
                }
            }
        
            if (isReset)
            {
                _contentRect.anchoredPosition = Vector2.zero;
                ManualRefreshItems(true);
            }
            else
            {
                ManualRefreshItems();
            }
        }
        
        public void RefreshItem(int index)
        {
            if (!_isInit || index < 0 || index >= _appointCount)
            {
                return;
            }
    
            var itemData = GetItem(index);
            if (itemData == null)
            {
                return;
            }
            
            _itemRefreshAction?.Invoke(itemData.Index, itemData.Widget);
        }
    
        private bool IsViewInner(ItemData itemData)
        {
            var itemPoint = _itemPoints[itemData.Index];
            var itemEndPoint = itemPoint + itemSize + (itemData.Index == _appointCount - 1 ? 0f : spacing);
            var contentPos = ContentPos;
            return contentPos + _viewSize >= itemPoint && contentPos <= itemEndPoint;
        }
        
        private void MoveTo(int index, bool isTween)
        {
            if (!_isInit)
            {
                return;
            }
    
            if (index < 0 || index >= _itemList.Count)
            {
                return;
            }
    
            var maxScrollablePos = (direction == DirectionType.Horizontal ? _contentRect.sizeDelta.x : _contentRect.sizeDelta.y)  - _viewSize;
            var itemPos = _itemList[index].Widget.Rect.anchoredPosition;
            var targetPos = direction == DirectionType.Horizontal ? itemPos.x : itemPos.y;
            targetPos = Mathf.Clamp(targetPos, 0, maxScrollablePos);
            
            var moveToPos = direction == DirectionType.Horizontal ? new Vector2(-targetPos, 0) : new Vector2(0, targetPos);
    
            if (isTween)
            {
                LiteRuntime.Action.AddAction(ActionBuilder.Sequence().RectTransformMove(_contentRect, moveToPos, 1f).Flush(), true);
            }
            else
            {
                _contentRect.anchoredPosition = moveToPos;
                ManualRefreshItems(true);
            }
        }
        
        public override void OnBeginDrag(PointerEventData eventData)
        {
            base.OnBeginDrag(eventData);
            _onDragBegin?.Invoke(this);
        }
        
        public override void OnDrag(PointerEventData eventData)
        {
            base.OnDrag(eventData);
            _onDragging?.Invoke(this);
        }
        
        public override void OnEndDrag(PointerEventData eventData)
        {
            base.OnEndDrag(eventData);
            _onDragEnd?.Invoke(this);
        }
    
        private void ManualRefreshItems(bool isRefreshIndex = false)
        {
            if (_appointCount == 0) return;
            
            RefreshBorder();
            
            if (isRefreshIndex)
            {
                var startPos = GetShowAreaStartPos();
                var startItemIndex = Mathf.Max(Mathf.FloorToInt(startPos / itemSize), 0);
                
                for (var i = 0; i < _itemList.Count; i++)
                {
                    var item = _itemList[i];
                    if (!item.Widget.Go.activeSelf) continue;
                    item.Index = startItemIndex + i;
                    SetItemPosition(item);
                    _itemRefreshAction(item.Index, item.Widget);
                }
                
                return;
            }
            
            foreach (var item in _itemList.Where(item => item.Widget.Go.activeSelf))
            {
                SetItemPosition(item);
                _itemRefreshAction(item.Index, item.Widget);
            }
        }

        private bool _isRefreshing;
        private void AutoRefreshItems()
        {
            if (_isRefreshing) return;
            
            var contentPos = ContentPos;
            var isLess = false;
            if (_minBorder != -1 && contentPos < _itemPoints[_minBorder])
            {
                isLess = true;
            }
            else if (_maxBorder != -1 && contentPos > _itemPoints[_maxBorder])
            {
                isLess = false;
            }
            else
            {
                return;
            }

            _isRefreshing = true;

            var minIndex = int.MaxValue;
            var maxIndex = int.MinValue;
            var changeList = new List<ItemData>();
            for (var i = 0; i < _itemList.Count; i++)
            {
                var item = _itemList[i];
                if (IsViewInner(item))
                {
                    minIndex = Mathf.Min(minIndex, item.Index);
                    maxIndex = Mathf.Max(maxIndex, item.Index);
                }
                else
                {
                    changeList.Add(item);
                }
            }

            foreach (var item in changeList)
            {
                if (isLess)
                {
                    minIndex -= 1;
                    if (minIndex < 0) break;
                    
                    item.Index = minIndex;
                }
                else
                {
                    maxIndex += 1;
                    if (maxIndex >= _appointCount) break;
                    
                    item.Index = maxIndex;
                }
                SetItemPosition(item);
                _itemRefreshAction(item.Index, item.Widget);
            }
            
            RefreshBorder();

            _isRefreshing = false;
        }

        private void RefreshBorder()
        {
            if (_appointCount == 0) return;

            var realCount = _itemList.Count > _appointCount ? _itemList.Count : _appointCount;
            var contentPos = ContentPos;
            if (contentPos <= _itemPoints[1])
            {
                _minBorder = -1;
                _maxBorder = 1;
                return;
            }
            if (contentPos >= _itemPoints[^1] + itemSize - _viewSize)
            {
                var count = Mathf.CeilToInt((_viewSize - spacing) / (itemSize + spacing));
                _minBorder = realCount - count;
                _maxBorder = -1;
                return;
            }

            if (_minBorder != -1 && contentPos < _itemPoints[_minBorder])
            {
                for (var i = _minBorder; i >= 0; i--)
                {
                    if (contentPos < _itemPoints[i]) continue;
                    _minBorder = i;
                    _maxBorder = i + 1;
                    return;
                }
            }
            else if (_maxBorder != -1 && contentPos > _itemPoints[_maxBorder])
            {
                for (var i = _maxBorder; i < realCount; i++)
                {
                    if (contentPos > _itemPoints[i]) continue;
                    _maxBorder = i;
                    _minBorder = i - 1;
                    return;
                }
            }
        }
        
        private void SetItemPosition(ItemData itemData)
        {
            if (direction == DirectionType.Vertical)
            {
                if (reverseArrangement)
                {
                    itemData.Widget.Rect.anchoredPosition = new Vector2(0, itemData.Index * (itemSize + spacing) + padding.bottom);
                }
                else
                {
                    itemData.Widget.Rect.anchoredPosition = new Vector2(0, -itemData.Index * (itemSize + spacing) - padding.top);
                }
            }
            else if (direction == DirectionType.Horizontal)
            {
                if (reverseArrangement)
                {
                    itemData.Widget.Rect.anchoredPosition = new Vector2(-itemData.Index * (itemSize + spacing) - padding.right, 0);
                }
                else
                {
                    itemData.Widget.Rect.anchoredPosition = new Vector2(itemData.Index * (itemSize + spacing) + padding.left, 0);
                }
            }
        }
    
        private float GetShowAreaStartPos()
        {
            var firstItemCenter = _itemPoints[0];
            return firstItemCenter - itemSize * 0.5f;
        }
    
        private ItemData GetItem(int index)
        {
            return _itemList.Find(t => t.Index == index);
        }
    }
}
