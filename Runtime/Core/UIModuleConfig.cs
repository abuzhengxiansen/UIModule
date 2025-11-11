using UnityEngine;

namespace GamePlay
{
    /// <summary>
    /// UI模块编辑器配置
    /// </summary>
    public class UIModuleConfig : ScriptableObject
    {
        [Header("脚本路径配置")]
        [Tooltip("UI配置脚本路径")]
        public string uiConfigsPath = "Assets/GamePlay/Config/UIConfigs.cs";
        
        [Tooltip("View脚本模板路径")]
        public string viewPresetScriptPath = "Assets/GamePlay/Editor/UI/UIViewTemplate.txt";
        
        [Tooltip("Widget脚本模板路径")]
        public string widgetPresetScriptPath = "Assets/GamePlay/Editor/UI/UIWidgetTemplate.txt";
        
        [Header("预制体路径配置")]
        [Tooltip("View预制体模板路径")]
        public string viewPrefabTemplatePath = "Assets/GamePlay/Editor/UI/UITemplate.prefab";
        
        [Tooltip("预制体文件夹路径")]
        public string prefabFolderPath = "Assets/GamePlay";
        
        [Header("预设配置")]
        [Tooltip("预设文件夹路径")]
        public string presetFolderPath = "Assets/Editor/PresetTemplate";
        
        [Header("UI根节点配置")]
        [Tooltip("UI根节点在场景中的路径")]
        public string uiRootPath = "Canvas";
        
        [Header("按钮点击音效资源默认播放参数")]
        [Tooltip("自身传入的播放回调对应的参数")]
        public string defalutClickAudio = "Audios/sd_btn_clock.wav";
        
        private static UIModuleConfig _instance;
        
        /// <summary>
        /// 获取配置实例
        /// </summary>
        public static UIModuleConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<UIModuleConfig>("UIModuleConfig");
                    if (_instance == null)
                    {
                        Debug.LogWarning("UIModuleConfig not found in Resources folder. Please create it via Tools/UI Module Config");
                    }
                }
                return _instance;
            }
        }
    }
}

