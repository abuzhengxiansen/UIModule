namespace GamePlay
{
    public class UIViewBinder : UIBaseBinder
    {
        [UnityEngine.Tooltip("UI层级名称，需要在UIModuleConfig中配置")]
        public string layer;
        public bool isMultiple;
        public bool isCoexist;
    }
}