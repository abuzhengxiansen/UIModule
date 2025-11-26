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
        [Tooltip("UI层级设置资源")]
        public List<UILayerDefine> layers;
        
        public UIModuleConfig()
        {
            uiConfigsPath = "";
            presetFolderPath = "";
            layers = new()
            {
                new UILayerDefine(1, "场景层", 100),
                new UILayerDefine(2, "Layer2未用", 200),
                new UILayerDefine(3, "背景层", 300),
                new UILayerDefine(4, "Layer4未用", 400),
                new UILayerDefine(5, "普通层", 500),
                new UILayerDefine(6, "弹窗层", 600),
                new UILayerDefine(7, "常驻", 700),
                new UILayerDefine(8, "引导", 800),
                new UILayerDefine(9, "顶层", 900),
            };
        }
    }
}

