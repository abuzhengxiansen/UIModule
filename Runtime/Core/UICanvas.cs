using UnityEngine;

namespace GamePlay
{
    /// <summary>
    /// UI画布组件，挂载在UI根节点上
    /// </summary>
    public class UICanvas : MonoBehaviour
    {
        [SerializeField]
        private UIModuleConfig config = new UIModuleConfig();
        
        /// <summary>
        /// 获取UI模块配置
        /// </summary>
        public UIModuleConfig Config => config;
    }
}