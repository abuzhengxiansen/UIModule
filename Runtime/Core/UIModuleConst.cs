using UnityEngine;

namespace GamePlay
{
    public enum UIStatus
    {
        Sleeping,               // 休眠
        Opening,                // 打开中
        Showing,                // 展示中
        Hiding,                 // 隐藏中
        Disposing,              // 销毁中
    }
    
    public enum LayerType
    {
        None,
        
        [InspectorName("场景层")]
        Layer1,
        
        [InspectorName("Layer2未用")]
        Layer2,
        
        [InspectorName("背景层")]
        Layer3,
        
        [InspectorName("Layer4未用")]
        Layer4,
        
        [InspectorName("普通层")]
        Layer5,
        
        [InspectorName("弹窗层")]
        Layer6,
        
        [InspectorName("常驻")]
        Layer7,
        
        [InspectorName("引导")]
        Layer8,
        
        [InspectorName("Layer9未用")]
        Layer9,
    }
    
    public enum UIOperateState
    {
        None,
        PrepareCreate,
        Created,
        PrepareOpen,
        Opened,
        PrepareClose,
        Closed,
        PrepareDispose,
        Disposed
    }
}