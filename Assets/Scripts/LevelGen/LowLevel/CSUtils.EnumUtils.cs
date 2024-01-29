using UnityEngine;

public static partial class CSUtils {
	/// <summary> Useful enum flag operations and checks for RenderTextures, like element-wise enum flag comparison, addition, and removal. </summary>
	/// <remarks> Note that none of the methods create new textures. If you want to leave the original texture intact, pass a copy of that texture as input. </remarks>
	public static class EnumUtils {
		static readonly ComputeShader cs = Resources.Load<ComputeShader>("EnumUtils");

		/// <summary> Forms a Binary Mask with whether pixels of the target contain the enum flag or not (`newVal = curVal.ContainsFlag(flag) ? 1 : 0`). </summary>
		public static RenderTexture ContainsFlag(RenderTexture target, float flag)	=> RunKernel(0, target, flag);

		/// <summary> Adds an enum flag to each pixel of the target, if it doesn't contain it already. </summary>
		public static RenderTexture AddFlag(RenderTexture target, float flag)		=> RunKernel(1, target, flag);

		/// <summary> Removes an enum flag to each pixel of the target, if it contains it. </summary>
		public static RenderTexture RemoveFlag(RenderTexture target, float flag)	=> RunKernel(2, target, flag);


		/// <summary> Executes a specific kernel with the multi-purpose 'value' float and returns the output -- which is the target itself. </summary>
		static RenderTexture RunKernel(int kernel, RenderTexture target, float value) {
			if (target.format != RenderTextureFormat.RHalf) { Debug.LogWarning("Only the X component is accounted in flag operations."); }

			cs.SetFloat("value", value);
			cs.SetTexture(kernel, "output", target);
			cs.Dispatch(kernel, target);
			return target;
		}
	}
}