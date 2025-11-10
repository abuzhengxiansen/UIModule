using UnityEngine.Rendering;

namespace UnityEngine.UI
{
    public class ReverseMaskImage : Image
    {
        /// <summary>
        ///   <para>See IMaterialModifier.GetModifiedMaterial.</para>
        /// </summary>
        /// <param name="baseMaterial"></param>
        public override Material GetModifiedMaterial(Material baseMaterial)
        {
            var baseMat = baseMaterial;
            if (this.m_ShouldRecalculateStencil)
            {
                var sortOverrideCanvas = MaskUtilities.FindRootSortOverrideCanvas(this.transform);
                this.m_StencilValue = !this.maskable ? 0 : MaskUtilities.GetStencilDepth(this.transform, sortOverrideCanvas);
                this.m_ShouldRecalculateStencil = false;
            }
            var component = this.GetComponent<Mask>();
            if (this.m_StencilValue > 0 && ((UnityEngine.Object) component == (UnityEngine.Object) null || !component.IsActive()))
            {
                Material material = StencilMaterial.Add(baseMat, (1 << this.m_StencilValue) - 1, StencilOp.Keep, CompareFunction.NotEqual, ColorWriteMask.All, (1 << this.m_StencilValue) - 1, 0);
                StencilMaterial.Remove(this.m_MaskMaterial);
                this.m_MaskMaterial = material;
                baseMat = this.m_MaskMaterial;
            }
            return baseMat;
        }
    }
}
