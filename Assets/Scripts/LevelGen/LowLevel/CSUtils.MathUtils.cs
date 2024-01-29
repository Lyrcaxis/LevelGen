using UnityEngine;

public static partial class CSUtils {
	/// <summary> Useful math operations for RenderTextures, like element-wise additions, substractions, multiplications, clamping, etc. Can also operate with multiple textures. </summary>
	/// <remarks> NOTE: None of the methods create new textures, and they operate on 'target'. If you want to leave the original texture intact, pass a copy of that texture as input. </remarks>
	public static class MathUtils {
		static readonly ComputeShader cs = Resources.Load<ComputeShader>("MathUtils");

		/// <summary> Adds a value to each pixel of the texture (`newVal = curVal - value`). </summary>
		public static RenderTexture AddValue(RenderTexture target, float value)			=> RunKernel(0, target, value);
		/// <summary> Removes a value from each pixel of the texture (`newVal = curVal - value`). </summary>
		public static RenderTexture SubtractValue(RenderTexture target, float value)	=> RunKernel(1, target, value);

		/// <summary> Multiplies the value of each pixel of the texture by specified value (`newVal = curVal * value`). </summary>
		public static RenderTexture ScaleValues(RenderTexture target, float value)		=> RunKernel(2, target, value);
		
		/// <summary> Clamps the texture to have a max value of 'max' (`newVal = min(curVal, value)`), and a min value of 'min' (`newVal = max(curVal, value)`). </summary>
		/// <remarks> Can specify only min or only max if only one of the operations are needed. </remarks>
		public static RenderTexture ClampValues(RenderTexture target, float? min = null, float? max = null) {
			if (min == null && max == null) { throw new("ClampValues needs at least one of 'min' or 'max' parameters set."); }
			if (max.HasValue) { RunKernel(3, target, max.Value); } // Apply Min -- keep min of 'max.Value' or 'curVal'.
			if (min.HasValue) { RunKernel(4, target, min.Value); } // Apply Max -- keep max of 'min.Value' or 'curVal'.
			return target;
		}

		/// <summary> Performs an element-wise addition from input texture to output texture (`newVal = curVal + value`). </summary>
		public static RenderTexture AddTexture(RenderTexture input, RenderTexture target)		=> RunKernel(5, input, target);

		/// <summary> Performs an element-wise subtraction from input texture to output texture (`newVal = curVal - value`). </summary>
		public static RenderTexture SubtractTexture(RenderTexture input, RenderTexture target)	=> RunKernel(6, input, target);

		/// <summary> Performs an element-wise multiplication from input texture to output texture (`newVal = curVal * value`). </summary>
		public static RenderTexture ScaleTexture(RenderTexture input, RenderTexture target)		=> RunKernel(7, input, target);



		/// <summary> Executes a specific kernel with the multi-purpose 'value' float and returns the output -- which is the target itself. </summary>
		static RenderTexture RunKernel(int kernel, RenderTexture target, float value) {
			cs.SetFloat("value", value);
			cs.SetTexture(kernel, "output", target);
			cs.Dispatch(kernel, target);
			return target;
		}
		/// <summary> Executes a specific kernel with specified input as parameter and returns the output -- which is the target itself. </summary>
		static RenderTexture RunKernel(int kernel, RenderTexture input, RenderTexture target) {
			cs.SetTexture(kernel, "input", input);
			cs.SetTexture(kernel, "output", target);
			cs.Dispatch(kernel, target);
			return target;
		}
	}
}