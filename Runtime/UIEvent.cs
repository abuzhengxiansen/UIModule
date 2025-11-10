using System;
using LiteQuark.Runtime;

namespace GamePlay
{
    public sealed class UIOperateEvent : IEventData
    {
        public string Name { get; }
        public Type Type { get; }
        public UIOperateState State { get; }
        public BaseView View { get; }
        
        public UIOperateEvent(BaseView view, UIOperateState operateState)
        {
            Type = view.Config.Type;
            Name = view.Config.Name;
            View = view;
            State = operateState;
        }
        
        public UIOperateEvent(Type type, string name, UIOperateState operateState)
        {
            Type = type;
            Name = name;
            State = operateState;
        }
    }
}