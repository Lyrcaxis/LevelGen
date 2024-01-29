using UnityEngine;

public static partial class CSUtils {
	public static class ComparisonUtils {
		static readonly ComputeShader cs = Resources.Load<ComputeShader>("ComparisonUtils");

		/// <summary> Turns target into a Binary Mask where pixels that have values equal to 'value' are 1s. </summary>
		public static RenderTexture Equals(RenderTexture target, float value)				=> DoFixedComparison(0, target, value);

		/// <summary> Turns target into a Binary Mask where pixels that have values not equal to 'value' are 1s. </summary>
		public static RenderTexture NotEquals(RenderTexture target, float value)			=> DoFixedComparison(1, target, value);

		/// <summary> Turns target into a Binary Mask where pixels that have values greater than 'value' are 1s. </summary>
		public static RenderTexture GreaterThan(RenderTexture target, float value)			=> DoFixedComparison(2, target, value);

		/// <summary> Turns target into a Binary Mask where pixels that have values greater than or equal to 'value' are 1s. </summary>
		public static RenderTexture GreaterThanOrEquals(RenderTexture target, float value)	=> DoFixedComparison(3, target, value);

		/// <summary> Turns target into a Binary Mask where pixels that have values smaller than 'value' are 1s. </summary>
		public static RenderTexture LessThan(RenderTexture output, float value)				=> DoFixedComparison(4, output, value);

		/// <summary> Turns target into a Binary Mask where pixels that have values smaller than or equal to 'value' are 1s. </summary>
		public static RenderTexture LessThanOrEquals(RenderTexture output, float value)		=> DoFixedComparison(5, output, value);


		/// <summary> Returns a new Binary Mask where values of 'a' are element-wise equal to values of 'b'. </summary>
		public static RenderTexture Equals(RenderTexture a, RenderTexture b)				=> DoTextureComparison(6, a, b);

		/// <summary> Returns a new Binary Mask where values of 'a' are element-wise not-equal to values of 'b'. </summary>
		public static RenderTexture NotEquals(RenderTexture a, RenderTexture b)				=> DoTextureComparison(7, a, b);

		/// <summary> Returns a new Binary Mask where values of 'a' are element-wise greater than values of 'b'. </summary>
		public static RenderTexture GreaterThan(RenderTexture a, RenderTexture b)			=> DoTextureComparison(8, a, b);

		/// <summary> Returns a new Binary Mask where values of 'a' are element-wise greater than or equal to values of 'b'. </summary>
		public static RenderTexture GreaterThanOrEquals(RenderTexture a, RenderTexture b)	=> DoTextureComparison(9, a, b);

		/// <summary> Returns a new Binary Mask where values of 'a' are element-wise smaller than values of 'b'. </summary>
		public static RenderTexture LessThan(RenderTexture a, RenderTexture b)				=> DoTextureComparison(10, a, b);

		/// <summary> Returns a new Binary Mask where values of 'a' are element-wise smaller than or equal to values of 'b'. </summary>
		public static RenderTexture LessThanOrEquals(RenderTexture a, RenderTexture b)		=> DoTextureComparison(11, a, b);



		static RenderTexture DoFixedComparison(int kernel, RenderTexture output, float value) {
			cs.SetFloat("value", value);
			cs.SetTexture(kernel, "output", output);
			return cs.Dispatch(kernel, output);
		}
		static RenderTexture DoTextureComparison(int kernel, RenderTexture a, RenderTexture b) {
			var output = CreateRT(a.width, a.height, RenderTextureFormat.RHalf);
			cs.SetTexture(kernel, "a", a);
			cs.SetTexture(kernel, "b", b);
			cs.SetTexture(kernel, "output", output);
			return cs.Dispatch(kernel, output);
		}
	}
}