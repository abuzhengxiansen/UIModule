using System;
using System.Collections.Generic;
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
        
        [Header("层级配置")]
        [Tooltip("UI层级名称列表，按顺序排列")]
        public List<string> layers;
        
        [Tooltip("层级之间的SortingOrder间隔")]
        [Range(100, 5000)]
        public int layerSortSpace;
        
        [Tooltip("同一层级内UI之间的SortingOrder间隔")]
        [Range(10, 100)]
        public int uiSortSpace;
        
        public UIModuleConfig()
        {
            uiConfigsPath = "";
            presetFolderPath = "";
            layerSortSpace = 1000;
            uiSortSpace = 100;
            layers = new List<string>();
        }
    }
}

