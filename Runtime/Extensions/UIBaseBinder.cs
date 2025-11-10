using System;
using UnityEngine.EventSystems;

namespace GamePlay
{
    public class UIBaseBinder : UIBehaviour
    {
        public string scriptDesc;
        public string creator;
        public UnityEngine.Object script;
        public string createTime;
    
        public bool showRulesFoldout; // 控制折叠
        public Action<string> AnimOverAction;
        public void UIAnimOver(string actionName)
        {
            AnimOverAction?.Invoke(actionName);
        }
    }
}