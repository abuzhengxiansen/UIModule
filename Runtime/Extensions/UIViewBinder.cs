namespace GamePlay
{
    public class UIViewBinder : UIBaseBinder
    {
        [UnityEngine.Tooltip("UI层级ID需要在UIModuleConfig中配置")]
        public int layer;
        public bool isMultiple;
        public bool isCoexist;
    }
}