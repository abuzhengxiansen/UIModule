using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    [RequireComponent(typeof(Selectable))]
    [DisallowMultipleComponent]
    public class ClickEffectSingle : UIBehaviour
    {
        [HideInInspector]
        public Selectable Selectable;

        public void SetSelectable(Selectable cur)
        {
            Selectable = cur;
        }
    }
}