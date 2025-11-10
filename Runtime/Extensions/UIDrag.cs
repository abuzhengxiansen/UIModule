using System;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    public class UIDrag : UIBehaviour, 
        IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerDownHandler
    {
        #region 内部
        public enum DragDirection { None, Up, Down, Left, Right }
        
        public bool interactable = true;
        public DragDirection directionType = DragDirection.None;
        public Vector2 maxRange;
        [Range(0, 90)] public float angleThreshold = 30;
        public float dragDelay;
        public RectTransform DragTarget => dragTarget;
        
        [SerializeField]
        private RectTransform dragTarget;
        private Action<UIDrag> _onClick;
        private Action<UIDrag, PointerEventData> _onDragBegin;
        private Action<UIDrag, PointerEventData> _onDragging;
        private Action<UIDrag, PointerEventData> _onDragEnd;
        
        private Vector2 _startPos;
        private float _dragStartTime;
        private bool _isValidDrag;
        private RectTransform _rectTransform;

        protected override void Awake() {
            _rectTransform = transform.GetComponent<RectTransform>();
            _startPos = _rectTransform.anchoredPosition;
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!interactable) return;
            if (Time.time - _dragStartTime > 0.2f) return;
            _onClick?.Invoke(this);
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!interactable) return;
            _dragStartTime = Time.time;
            _startPos = _rectTransform.anchoredPosition;
        }
    
        public void OnBeginDrag(PointerEventData eventData) 
        {
            if (!interactable) return;
            if (Time.time - _dragStartTime < dragDelay) return;
    
            var dragVector = eventData.delta.normalized;
            if (!CheckDragAngle(dragVector)) return;
    
            _isValidDrag = true;
            _onDragBegin?.Invoke(this, eventData);
        }
    
        public void OnDrag(PointerEventData eventData) 
        {
            if (!_isValidDrag) return;
            
            RefreshTargetPos(eventData);
            _onDragging?.Invoke(this, eventData);
        }
    
        public void OnEndDrag(PointerEventData eventData) 
        {
            _isValidDrag = false;
            _onDragEnd?.Invoke(this, eventData);
        }
        
        private void RefreshTargetPos(PointerEventData eventData) 
        {
            if (dragTarget == null) return;
            
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, eventData.position,
                eventData.pressEventCamera, out var localPoint);
            localPoint = ApplyDragConstraints(localPoint);
            dragTarget.anchoredPosition = localPoint;
        }
    
        private bool CheckDragAngle(Vector2 dragVector) 
        {
            if (directionType == DragDirection.None) return true;
    
            float targetAngle = directionType switch {
                DragDirection.Up => 90,
                DragDirection.Down => 270,
                DragDirection.Left => 180,
                DragDirection.Right => 0,
                _ => 0
            };
    
            var currentAngle = Vector2.Angle(Vector2.right, dragVector);
            return Mathf.Abs(currentAngle - targetAngle) <= angleThreshold;
        }
    
        private Vector2 ApplyDragConstraints(Vector2 localPos) 
        {
            if (maxRange == Vector2.zero) return localPos;
            
            switch (directionType)
            {
                case DragDirection.Up:
                    localPos.x = Mathf.Clamp(localPos.x, - maxRange.x * 0.5f, maxRange.x * 0.5f);
                    localPos.y = Mathf.Clamp(localPos.y, 0f, maxRange.y);
                    break;
                case DragDirection.Down:
                    localPos.x = Mathf.Clamp(localPos.x, - maxRange.x * 0.5f, maxRange.x * 0.5f);
                    localPos.y = Mathf.Clamp(localPos.y, - maxRange.y, 0f);
                    break;
                case DragDirection.Left:
                    localPos.x = Mathf.Clamp(localPos.x, - maxRange.x, 0f);
                    localPos.y = Mathf.Clamp(localPos.y, - maxRange.y * 0.5f, maxRange.y * 0.5f);
                    break;
                case DragDirection.Right:
                    localPos.x = Mathf.Clamp(localPos.x, 0f, maxRange.x);
                    localPos.y = Mathf.Clamp(localPos.y, - maxRange.y * 0.5f, maxRange.y * 0.5f);
                    break;
                case DragDirection.None:
                    localPos.x = Mathf.Clamp(localPos.x, - maxRange.x * 0.5f, maxRange.x * 0.5f);
                    localPos.y = Mathf.Clamp(localPos.y, - maxRange.y * 0.5f, maxRange.y * 0.5f);
                    break;
            }
            return localPos;
        }
        
        #endregion
    
        #region 对外
    
        public void SetClickEvent(Action<UIDrag> action)
        {
            _onClick = action;
        }
        
        public void SetDragBeginEvent(Action<UIDrag, PointerEventData> action)
        {
            _onDragBegin = action;
        }
        
        public void SetDraggingEvent(Action<UIDrag, PointerEventData> action)
        {
            _onDragging = action;
        }
        
        public void SetDragEndEvent(Action<UIDrag, PointerEventData> action)
        {
            _onDragEnd = action;
        }
    
        #endregion
    }
}
