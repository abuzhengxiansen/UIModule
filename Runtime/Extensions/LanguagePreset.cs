using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace GamePlay
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LanguagePreset : MonoBehaviour
    {
        [Tooltip("多语言键值ID")] [SerializeField] private string languageKey;

        [Tooltip("可选格式化参数(例如: {0})")] [SerializeField]
        private List<string> formatArgs = new();

        private TextMeshProUGUI _textComponent;

        void Awake()
        {
            _textComponent = GetComponent<TextMeshProUGUI>();

            if (_textComponent == null)
            {
                Debug.LogError("LanguagePreset requires a TextMeshProUGUI component!", this);
                enabled = false;
                return;
            }

            // 初始加载文本
            RefreshText();
        }

        /// <summary>
        /// 刷新UI文本内容
        /// </summary>
        public void RefreshText()
        {
            if (!gameObject.activeInHierarchy || _textComponent == null)
                return;

            if (LangManager.Instance == null)
            {
                Debug.LogWarning("LangManager instance not found!");
                return;
            }

            // 获取多语言文本
            var text = LangManager.Instance.GetText(languageKey);
            
            // 应用格式化参数
            if (text != null && formatArgs.Count > 0)
            {
                text = string.Format(text, formatArgs.ToArray());
            }

            if (text == null)
            {
                text = languageKey;
            }

            // 更新文本组件
            _textComponent.text = text;
        }
    }
}