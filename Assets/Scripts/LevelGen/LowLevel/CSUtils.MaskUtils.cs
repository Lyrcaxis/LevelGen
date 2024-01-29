using UnityEngine;

public static partial class CSUtils {
	public static class MaskUtils {
		static readonly ComputeShader cs = Resources.Load<ComputeShader>("MaskUtils");

		/// <summary> Applies the input mask to the target, discarding values where mask is 0. </summary>
		public static RenderTexture Intersect(RenderTexture mask, RenderTexture target)			=> RunKernel(0, mask, target);

		/// <summary> Bakes the input mask into the target, putting 1s where mask is not 0. </summary>
		public static RenderTexture IntersectAdd(RenderTexture mask, RenderTexture target)		=> RunKernel(1, mask, target);

		/// <summary> Removes the input mask from the target, discarding values where mask is not 0. </summary>
		/// <remarks> This is functionally equivalent to `Intersect(inverseMask)`. </remarks>
		public static RenderTexture IntersectRemove(RenderTexture mask, RenderTexture target)	=> RunKernel(2, mask, target);

		/// <summary> Inverts the input mask, setting 0s where 1s were present, and vice versa. Returns the same mask. </summary>
		public static RenderTexture Invert(RenderTexture mask)									=> RunKernel(3, mask, mask);


		/// <summary> Executes a specific kernel with a specific value and returns the output -- which is the target itself. </summary>
		static RenderTexture RunKernel(int kernel, RenderTexture mask, RenderTexture target) {
			cs.SetTexture(kernel, "mask", mask);
			cs.SetTexture(kernel, "output", target);
			cs.Dispatch(kernel, target);
			return target;
		}
	}
}