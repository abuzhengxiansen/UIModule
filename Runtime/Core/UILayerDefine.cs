using System;
using UnityEngine;

namespace GamePlay
{
    /// <summary>
    /// UI层级定义，用于配置自定义层级
    /// </summary>
    [Serializable]
    public class UILayerDefine
    {
        [Tooltip("层级ID"), SerializeField]
        public int layerId;
        
        [Tooltip("层级显示名称"), SerializeField]
        public string layerName;
        
        [Tooltip("层级排序值"), SerializeField]
        public int layerOrder;

        public UILayerDefine(int id, string name, int order)
        {
            layerId = id;
            layerName = name;
            layerOrder = order;
        }
    }
}

