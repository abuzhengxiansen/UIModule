using System;
using GamePlay;
using LiteQuark.Runtime;
using UnityEngine.EventSystems;
using TMPro;

namespace UnityEngine.UI
{
    [RequireComponent(typeof(Image))]
    public class UISlider : Selectable, IDragHandler, IInitializePotentialDragHandler
    {
        #region 内部

        public Image fill;
        public TMP_Text valueText;
        public Image handle;
        public float Progress => Mathf.InverseLerp(minValue, maxValue, currentValue);
    
        [SerializeField] private string valueFormat = "{0}/{1}";
        [SerializeField] private float minValue;
        [SerializeField] private float maxValue = 1;
        [SerializeField] private float currentValue;
        [SerializeField] private bool isWholeValue;
        [SerializeField] private bool showDetailSetting;
    
        private Action<UISlider, float, float> _onChangeEvent;
        
        private void UpdateVisuals()
        {
            UpdateFill();
            UpdateHandle();
            UpdateValueText();
        }
        
        private void UpdateFill()
        {
            if (fill == null)
            {
                LiteRuntime.Get<UIModule>()?.LogError($"UISlider {name} Fill is null");
                return;
            }
            if (fill.sprite != null && fill.type != Image.Type.Filled)
            {
                LiteRuntime.Get<UIModule>()?.LogError($"UISlider {name} Fill image type need be Filled");
                return;
            }
            fill.fillAmount = Progress;
        }
        
        private void UpdateHandle()
        {
            if (handle == null) return;
            handle.rectTransform.anchoredPosition = new Vector2(
                fill.rectTransform.rect.width * Progress,
                handle.rectTransform.anchoredPosition.y
            );
        }
    
        private void UpdateValueText()
        {
            if (valueText == null) return;
            valueText.text = string.Format(valueFormat, currentValue, maxValue);
        }
    
        public override void OnPointerDown(PointerEventData eventData)
        {
            if (!IsInteractable()) return;
            base.OnPointerDown(eventData);
            UpdateDrag(eventData);
        }
    
        public void OnDrag(PointerEventData eventData)
        {
            if (!IsInteractable()) return;
            UpdateDrag(eventData);
        }
    
        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = false;
        }
    
        private void UpdateDrag(PointerEventData eventData)
        {
            var fillRect = fill.rectTransform;
            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    fillRect, eventData.position, eventData.pressEventCamera, out localPoint))
                return;
        
            var progress = Mathf.Clamp01((localPoint.x / fillRect.rect.width) + 0.5f);
            var value = Mathf.Lerp(minValue, maxValue, progress);
            SetValue(value);
        }

        #endregion

        #region 对外

        public void SetRange(float min, float max)
        {
            minValue = min;
            maxValue = max;
            SetValue(currentValue);
        }
    
        public void SetValue(float value)
        {
            if (isWholeValue)
            {
                value = Mathf.Round(value);
            }
            currentValue = Mathf.Clamp(value, minValue, maxValue);
            UpdateVisuals();
            _onChangeEvent?.Invoke(this, Progress, currentValue);
        }
    
        public void SetProgress(float progress)
        {
            SetValue(Mathf.Lerp(minValue, maxValue, progress));
        }
    
        public void SetValueDesc(string format)
        {
            valueFormat = format;
            UpdateValueText();
        }

        public void SetChangeEvent(Action<UISlider, float, float> action)
        {
            _onChangeEvent = action;
        }
        
        public float GetValue() => currentValue;
        public float GetProgress() => Progress;
        public (float, float) GetRange() => (minValue, maxValue);

        #endregion
    }
}
