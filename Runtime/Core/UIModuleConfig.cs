using System;
using UnityEngine;

namespace GamePlay
{
    /// <summary>
    /// UI模块配置
    /// </summary>
    [Serializable]
    public class UIModuleConfig
    {
        [Header("脚本路径配置")]
        [Tooltip("UI配置脚本路径")]
        public string uiConfigsPath;
        
        [Header("预设配置")]
        [Tooltip("预设文件夹路径")]
        public string presetFolderPath;
        
        [Header("按钮点击音效资源默认播放参数")]
        [Tooltip("自身传入的播放回调对应的参数")]
        public string defaultClickAudio = "Audios/sd_btn_clock.wav";
    }
}

